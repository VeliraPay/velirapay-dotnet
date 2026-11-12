namespace VeliraPay;

/// <summary>The API answered with something this library cannot read, such as a successful answer that is not JSON.</summary>
public sealed class UnexpectedResponseException : ApiException
{
    /// <inheritdoc cref="ApiException(string, ApiResponse)"/>
    public UnexpectedResponseException(string message, ApiResponse response)
        : base(message, response)
    {
    }
}
