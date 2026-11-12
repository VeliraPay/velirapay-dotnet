namespace VeliraPay;

/// <summary>The API key is missing, wrong or revoked (401).</summary>
public sealed class AuthenticationException : ApiException
{
    /// <inheritdoc cref="ApiException(string, ApiResponse)"/>
    public AuthenticationException(string message, ApiResponse response)
        : base(message, response)
    {
    }
}
