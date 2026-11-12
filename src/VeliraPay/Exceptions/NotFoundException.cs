namespace VeliraPay;

/// <summary>The object does not exist for this account and mode (404).</summary>
public sealed class NotFoundException : ApiException
{
    /// <inheritdoc cref="ApiException(string, ApiResponse)"/>
    public NotFoundException(string message, ApiResponse response)
        : base(message, response)
    {
    }
}
