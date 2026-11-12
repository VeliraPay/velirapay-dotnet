using System.Globalization;
using System.Net.Http.Headers;

namespace VeliraPay;

/// <summary>Sends requests to the VeliraPay API and turns its answers into responses or exceptions.</summary>
internal sealed class HttpTransport
{
    /// <summary>The statuses worth another attempt for any request.</summary>
    private static readonly int[] RetryableStatuses = [429, 502, 503, 504];

    /// <summary>What .NET calls a failed request when it says nothing more.</summary>
    private const string GenericSendFailure = "An error occurred while sending the request.";

    private readonly HttpClient httpClient;
    private readonly string apiKey;
    private readonly string baseUrl;
    private readonly int maxRetries;
    private readonly TimeSpan maxRetryAfter;
    private readonly TimeSpan timeout;
    private readonly string userAgent;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;
    private ApiResponse? lastResponse;

    /// <summary>Create a new transport, which waits between attempts with Task.Delay unless given a delay, as tests do to record the waits.</summary>
    public HttpTransport(HttpClient httpClient, string apiKey, string baseUrl, int maxRetries, TimeSpan maxRetryAfter, TimeSpan timeout, string userAgent, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        this.httpClient = httpClient;
        this.apiKey = apiKey;
        this.baseUrl = baseUrl.TrimEnd('/');
        this.maxRetries = maxRetries;
        this.maxRetryAfter = maxRetryAfter;
        this.timeout = timeout;
        this.userAgent = userAgent;
        this.delay = delay ?? ((duration, token) => Task.Delay(duration, token));
    }

    /// <summary>The response to the most recent request, whichever finished last.</summary>
    public ApiResponse? LastResponse => Volatile.Read(ref lastResponse);

    /// <summary>Send a request and return the successful response: the query parameters go in its URL, nulls left out, and the body parameters, when given, in its JSON body.</summary>
    public async Task<ApiResponse> RequestAsync(HttpMethod method, string path, IEnumerable<KeyValuePair<string, object?>>? query, IEnumerable<KeyValuePair<string, object?>>? body, string? idempotencyKey, CancellationToken cancellationToken)
    {
        method = Normalize(method);

        if (idempotencyKey is not null && (idempotencyKey.Length == 0 || !ParamsEncoder.IsHeaderSafe(idempotencyKey, allowSpaces: true)))
        {
            throw new InvalidArgumentException("The idempotency key cannot be sent as a header: it must be printable ASCII text, such as an order number, and not empty.");
        }

        // A retried write must not be carried out twice, so every attempt carries the same key.
        if (method != HttpMethod.Get && idempotencyKey is null && maxRetries > 0)
        {
            idempotencyKey = Guid.NewGuid().ToString();
        }

        var url = Url(path, query);
        var payload = body is null ? null : ParamsEncoder.Json(body);
        var attempt = 0;

        while (true)
        {
            ApiResponse response;

            try
            {
                response = await SendAsync(method, url, payload, idempotencyKey, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (IsNetworkError(exception, cancellationToken))
            {
                if (attempt >= maxRetries)
                {
                    throw new ConnectionException($"Could not reach the VeliraPay API: {Reason(exception)}", exception);
                }

                await delay(Backoff(attempt++), cancellationToken).ConfigureAwait(false);

                continue;
            }

            Volatile.Write(ref lastResponse, response);

            if (response.IsSuccessful)
            {
                return response;
            }

            var wait = RetryDelay(method, response, attempt);

            if (wait is null)
            {
                throw ApiException.FromResponse(response);
            }

            await delay(wait.Value, cancellationToken).ConfigureAwait(false);
            attempt++;
        }
    }

    /// <summary>Send one attempt of a request, and read the whole answer within the timeout.</summary>
    private async Task<ApiResponse> SendAsync(HttpMethod method, string url, byte[]? payload, string? idempotencyKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

        if (payload is not null)
        {
            request.Content = new ByteArrayContent(payload);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        if (idempotencyKey is not null)
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        }

        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (timeout != Timeout.InfiniteTimeSpan)
        {
            attempt.CancelAfter(timeout);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, attempt.Token).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(attempt.Token).ConfigureAwait(false);

            return new ApiResponse((int)response.StatusCode, Headers(response), body);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested && attempt.IsCancellationRequested)
        {
            throw new TimeoutException($"no answer within {Seconds(timeout)} seconds.", exception);
        }
    }

    /// <summary>How long to wait before trying a failed request again, or null when it should not be retried.</summary>
    private TimeSpan? RetryDelay(HttpMethod method, ApiResponse response, int attempt)
    {
        if (attempt >= maxRetries)
        {
            return null;
        }

        var retryAfter = response.RetryAfter;

        // A conflict that says when to come back is an earlier request with the same idempotency key still being processed.
        var retryable = RetryableStatuses.Contains(response.Status)
            || (response.Status == 500 && method == HttpMethod.Get)
            || (response.Status == 409 && retryAfter is not null);

        if (!retryable)
        {
            return null;
        }

        if (retryAfter is null)
        {
            return Backoff(attempt);
        }

        return retryAfter <= maxRetryAfter ? retryAfter : null;
    }

    /// <summary>The address of a path, with its query string.</summary>
    private string Url(string path, IEnumerable<KeyValuePair<string, object?>>? query)
    {
        var url = $"{baseUrl}/{path.TrimStart('/')}";
        var queryString = query is null ? string.Empty : ParamsEncoder.Query(query);

        if (queryString.Length == 0)
        {
            return url;
        }

        return url + (url.Contains('?', StringComparison.Ordinal) ? '&' : '?') + queryString;
    }

    /// <summary>Whether sending failed to reach the API, or to read its answer, rather than the code calling it failing.</summary>
    private static bool IsNetworkError(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        HttpRequestException or IOException or TimeoutException => true,
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false,
    };

    /// <summary>Say why a request could not reach the API; an HttpClient's own timeout says so in its message.</summary>
    private static string Reason(Exception exception)
    {
        if (exception.Message == GenericSendFailure && exception.InnerException is not null)
        {
            return exception.GetBaseException().Message;
        }

        return exception.Message;
    }

    /// <summary>The wait before an attempt: half a second, doubling each time up to 8, with some jitter.</summary>
    private static TimeSpan Backoff(int attempt) => TimeSpan.FromSeconds(Math.Min(8, 0.5 * Math.Pow(2, attempt)) * (0.75 + (Random.Shared.NextDouble() * 0.5)));

    /// <summary>Read every header of a response, its content's included, as it was received.</summary>
    private static Dictionary<string, IReadOnlyList<string>> Headers(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, values) in response.Headers.NonValidated.Concat(response.Content.Headers.NonValidated))
        {
            headers[name] = [.. values];
        }

        return headers;
    }

    /// <summary>A method in capitals, as HTTP names it.</summary>
    private static HttpMethod Normalize(HttpMethod method)
    {
        var name = method.Method.ToUpperInvariant();

        return name == method.Method ? method : new HttpMethod(name);
    }

    /// <summary>A number of seconds, as people write it.</summary>
    private static string Seconds(TimeSpan duration) => duration.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
}
