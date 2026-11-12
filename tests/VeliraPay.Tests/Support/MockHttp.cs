using System.Net;
using System.Text;

namespace VeliraPay.Tests.Support;

/// <summary>An HTTP handler that answers with queued responses and records every request, so nothing reaches the network.</summary>
internal sealed class MockHttp : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> queue = new();

    /// <summary>The requests sent so far.</summary>
    public List<RecordedRequest> Requests { get; } = [];

    /// <summary>The most recent request.</summary>
    public RecordedRequest LastRequest => Requests.Count > 0 ? Requests[^1] : throw new InvalidOperationException("No request was sent.");

    /// <summary>Queue a JSON answer.</summary>
    public MockHttp Json(JsonNode body, int status = 200, params (string Name, string Value)[] headers) => Json(body.ToJsonString(), status, headers);

    /// <summary>Queue a JSON answer written out.</summary>
    public MockHttp Json(string body, int status = 200, params (string Name, string Value)[] headers) => Text(body, status, "application/json", headers);

    /// <summary>Queue an answer of any content type.</summary>
    public MockHttp Text(string body, int status, string contentType, params (string Name, string Value)[] headers)
        => Answer((_, _) => Task.FromResult(Response(status, new StringContent(body, Encoding.UTF8, contentType), headers)));

    /// <summary>Queue an answer without a body, such as a 204.</summary>
    public MockHttp Empty(int status = 204) => Answer((_, _) => Task.FromResult(Response(status, null, [])));

    /// <summary>Queue an error the handler throws instead of answering.</summary>
    public MockHttp Throw(Exception exception) => Answer((_, _) => Task.FromException<HttpResponseMessage>(exception));

    /// <summary>Queue an answer worked out from the request.</summary>
    public MockHttp Answer(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> answer)
    {
        queue.Enqueue(answer);

        return this;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(await RecordedRequest.From(request));

        if (!queue.TryDequeue(out var answer))
        {
            throw new InvalidOperationException($"No response was queued for {request.Method} {request.RequestUri}.");
        }

        return await answer(request, cancellationToken);
    }

    /// <summary>Build a response.</summary>
    private static HttpResponseMessage Response(int status, HttpContent? content, (string Name, string Value)[] headers)
    {
        var response = new HttpResponseMessage((HttpStatusCode)status) { Content = content ?? new ByteArrayContent([]) };

        foreach (var (name, value) in headers)
        {
            if (!response.Headers.TryAddWithoutValidation(name, value))
            {
                response.Content.Headers.TryAddWithoutValidation(name, value);
            }
        }

        return response;
    }
}

/// <summary>What a request carried, copied before the request is disposed.</summary>
internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, string? Body)
{
    /// <summary>The request's path, as sent.</summary>
    public string Path => Uri.AbsolutePath;

    /// <summary>The request's query string, without its question mark.</summary>
    public string Query => Uri.Query.TrimStart('?');

    /// <summary>The request's query parameters, decoded.</summary>
    public Dictionary<string, string> QueryParameters => Query.Length == 0
        ? []
        : Query.Split('&').Select(pair => pair.Split('=', 2)).ToDictionary(pair => Uri.UnescapeDataString(pair[0]), pair => Uri.UnescapeDataString(pair.Length > 1 ? pair[1] : string.Empty));

    /// <summary>The request's JSON body, decoded.</summary>
    public JsonNode? Json => Body is null ? null : JsonNode.Parse(Body);

    /// <summary>A header's value, or null when the request had none.</summary>
    public string? Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;

    /// <summary>Copy what a request carries.</summary>
    public static async Task<RecordedRequest> From(HttpRequestMessage request)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, values) in request.Headers.NonValidated)
        {
            headers[name] = string.Join(", ", values);
        }

        string? body = null;

        if (request.Content is not null)
        {
            foreach (var (name, values) in request.Content.Headers.NonValidated)
            {
                headers[name] = string.Join(", ", values);
            }

            body = await request.Content.ReadAsStringAsync();
        }

        return new RecordedRequest(request.Method, request.RequestUri!, headers, body);
    }
}
