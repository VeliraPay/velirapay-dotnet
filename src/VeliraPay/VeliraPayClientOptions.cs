namespace VeliraPay;

/// <summary>How a <see cref="VeliraPayClient"/> reaches the API. Every property has a sensible default.</summary>
public sealed class VeliraPayClientOptions
{
    /// <summary>The API's address; <see cref="VeliraPayClient.DefaultBaseUrl"/> unless given.</summary>
    public string BaseUrl { get; set; } = VeliraPayClient.DefaultBaseUrl;

    /// <summary>
    /// How many times a request is tried again after a network error, a rate limit, an outage, or while one with the same idempotency key is still being processed.
    /// 0 turns retries off. 2 unless given.
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>The longest Retry-After that is waited out before trying again; a longer one is thrown straight away. 10 seconds unless given.</summary>
    public TimeSpan MaxRetryAfter { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long one attempt may take, from sending the request to reading the whole answer. 30 seconds unless given;
    /// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> leaves it to the <see cref="HttpClient.Timeout"/> of the client in use, which applies too.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Your application's name and version, such as "MyShop/2.1", added to the User-Agent.</summary>
    public string? AppInfo { get; set; }
}
