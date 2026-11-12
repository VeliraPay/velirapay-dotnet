namespace VeliraPay;

/// <summary>Options for one request that changes something.</summary>
public sealed class RequestOptions
{
    /// <summary>
    /// Up to 255 characters, such as an order number. For 24 hours, a repeat with the same key, for the same account, mode, method and path,
    /// gets the first successful response instead of being carried out again. Without one, the library makes one up per call while retries are on.
    /// </summary>
    public string? IdempotencyKey { get; set; }
}
