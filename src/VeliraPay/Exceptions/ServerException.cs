namespace VeliraPay;

/// <summary>The API failed to handle the request (5xx).</summary>
public sealed class ServerException : ApiException
{
    /// <inheritdoc cref="ApiException(string, ApiResponse)"/>
    public ServerException(string message, ApiResponse response)
        : base(message, response)
    {
    }
}
