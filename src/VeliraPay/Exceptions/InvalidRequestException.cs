namespace VeliraPay;

/// <summary>The request was malformed (400 and any other client error), or the API answered with a redirect, which is not followed.</summary>
public sealed class InvalidRequestException : ApiException
{
    /// <inheritdoc cref="ApiException(string, ApiResponse)"/>
    public InvalidRequestException(string message, ApiResponse response)
        : base(message, response)
    {
    }
}
