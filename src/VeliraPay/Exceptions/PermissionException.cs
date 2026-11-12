namespace VeliraPay;

/// <summary>The API key may not do what was asked (403): a restricted key without the permission the endpoint needs, or a key used from an address it does not allow.</summary>
public sealed class PermissionException : ApiException
{
    /// <inheritdoc cref="ApiException(string, ApiResponse)"/>
    public PermissionException(string message, ApiResponse response)
        : base(message, response)
    {
    }
}
