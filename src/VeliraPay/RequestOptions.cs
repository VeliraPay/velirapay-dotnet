namespace VeliraPay;

/// <summary>Options for one request that changes something.</summary>
public sealed class RequestOptions
{
    /// <summary>
    /// Up to 255 characters of printable ASCII, such as "order-1042-attempt-1". For 24 hours, a repeat with the same key, for the same account, mode, method and path,
    /// gets the first successful response instead of being carried out again. Reuse a key only to retry the same request: a new attempt,
    /// such as a charge replacing one that expired, needs a new key, or it gets the old charge back. Without one, the library makes one up per call while retries are on.
    /// </summary>
    public string? IdempotencyKey { get; set; }
}
