namespace VeliraPay;

/// <summary>Too many requests were made, or a limit such as emailing an invoice again was reached (429). <see cref="ApiException.RetryAfter"/> says how long to wait.</summary>
public sealed class RateLimitException : ApiException
{
    /// <inheritdoc cref="ApiException(string, ApiResponse)"/>
    public RateLimitException(string message, ApiResponse response)
        : base(message, response)
    {
    }
}
