namespace VeliraPay;

/// <summary>The object's state does not allow the action, or an idempotency key was reused for a different request or is held by one still being processed (409).</summary>
public sealed class ConflictException : ApiException
{
    /// <inheritdoc cref="ApiException(string, ApiResponse)"/>
    public ConflictException(string message, ApiResponse response)
        : base(message, response)
    {
    }
}
