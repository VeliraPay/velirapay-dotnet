using System.Text.Json;

namespace VeliraPay;

/// <summary>The requests shared by every group of endpoints.</summary>
public abstract class Service
{
    /// <summary>Create a group of endpoints that sends its requests through a transport.</summary>
    private protected Service(HttpTransport transport)
    {
        Transport = transport;
    }

    /// <summary>Sends the requests.</summary>
    private protected HttpTransport Transport { get; }

    /// <summary>Send a request and return the object under the response's "data" key.</summary>
    private protected async Task<JsonElement> RequestAsync(HttpMethod method, string path, BaseOptions? body, RequestOptions? requestOptions, CancellationToken cancellationToken)
        => Data(await RespondAsync(method, path, body, requestOptions, cancellationToken).ConfigureAwait(false));

    /// <summary>Send a request and return the whole response, for answers whose meta matters too.</summary>
    private protected Task<ApiResponse> RespondAsync(HttpMethod method, string path, BaseOptions? body, RequestOptions? requestOptions, CancellationToken cancellationToken)
        => Transport.RequestAsync(method, path, null, body?.Parameters, requestOptions?.IdempotencyKey, cancellationToken);

    /// <summary>Fetch one page of a list.</summary>
    private protected async Task<Page<T>> PageAsync<T>(string path, IReadOnlyList<KeyValuePair<string, object?>> query, Func<JsonElement, T> hydrate, CancellationToken cancellationToken)
        where T : ApiResource
    {
        var response = await Transport.RequestAsync(HttpMethod.Get, path, query, null, null, cancellationToken).ConfigureAwait(false);
        var data = List(response).Select(hydrate).ToList();
        var meta = Meta(response);
        var currentPage = JsonValues.TryGet(meta, "current_page", out var current) ? JsonValues.Integer(current) ?? 1 : 1;

        return new Page<T>(
            data,
            currentPage,
            JsonValues.TryGet(meta, "last_page", out var last) ? JsonValues.Integer(last) ?? currentPage : currentPage,
            JsonValues.TryGet(meta, "per_page", out var perPage) ? JsonValues.Integer(perPage) ?? data.Count : data.Count,
            JsonValues.TryGet(meta, "total", out var total) ? JsonValues.Integer(total) ?? data.Count : data.Count,
            meta,
            (page, token) => PageAsync(path, WithPage(query, page), hydrate, token));
    }

    /// <summary>The parameters of a list request, copied so later changes to the options do not reach the pages after it.</summary>
    private protected static IReadOnlyList<KeyValuePair<string, object?>> Query(BaseOptions? options) => options is null ? [] : [.. options.Parameters];

    /// <summary>Read the object under a response's "data" key.</summary>
    private protected static JsonElement Data(ApiResponse response)
    {
        if (!JsonValues.TryGet(response.Json(), "data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            throw new UnexpectedResponseException("The VeliraPay API answered without the object it was asked for.", response);
        }

        return data;
    }

    /// <summary>Read what is under a response's "meta" key.</summary>
    private protected static JsonElement Meta(ApiResponse response)
        => JsonValues.TryGet(response.Json(), "meta", out var meta) && meta.ValueKind == JsonValueKind.Object ? meta : JsonValues.EmptyObject;

    /// <summary>Read the objects of the list under a response's "data" key.</summary>
    private protected static IEnumerable<JsonElement> List(ApiResponse response)
    {
        if (!JsonValues.TryGet(response.Json(), "data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            throw new UnexpectedResponseException("The VeliraPay API answered without the list it was asked for.", response);
        }

        return data.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object);
    }

    /// <summary>Build a path with the given ids escaped into its {id} placeholders, in order.</summary>
    /// <exception cref="InvalidArgumentException">An id is empty, or one a URL would read as a directory.</exception>
    private protected static string Path(string template, params string?[] ids)
    {
        var path = template;

        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidArgumentException($"The id in {template} cannot be empty.");
            }

            // A URL reads these as the current and parent path, and would send the request elsewhere.
            if (id is "." or "..")
            {
                throw new InvalidArgumentException($"The id in {template} cannot be \"{id}\".");
            }

            var at = path.IndexOf("{id}", StringComparison.Ordinal);
            path = string.Concat(path.AsSpan(0, at), Uri.EscapeDataString(id), path.AsSpan(at + 4));
        }

        return path;
    }

    /// <summary>The parameters of a list request with another page.</summary>
    private static List<KeyValuePair<string, object?>> WithPage(IReadOnlyList<KeyValuePair<string, object?>> query, int page)
    {
        var parameters = new List<KeyValuePair<string, object?>>(query);
        var index = parameters.FindIndex(parameter => parameter.Key == "page");

        if (index >= 0)
        {
            parameters[index] = new KeyValuePair<string, object?>("page", page);
        }
        else
        {
            parameters.Add(new KeyValuePair<string, object?>("page", page));
        }

        return parameters;
    }
}
