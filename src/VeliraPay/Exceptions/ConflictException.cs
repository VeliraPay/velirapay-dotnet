namespace VeliraPay;

/// <summary>
/// The object's state does not allow the action, or an idempotency key was reused for a different request or is held by one still being processed (409).
/// Also the answer to repeating a write whose answer carried a secret, which is shown once and never replayed: see <see cref="WasReplayed"/>.
/// </summary>
public sealed class ConflictException : ApiException
{
    /// <inheritdoc cref="ApiException(string, ApiResponse)"/>
    public ConflictException(string message, ApiResponse response)
        : base(message, response)
    {
    }

    /// <summary>
    /// Whether the API answered from an earlier request with the same idempotency key, which already succeeded but whose answer carried a secret:
    /// a new API key, or a webhook endpoint's signing secret. The key or the endpoint was created, or the secret rotated, but the secret itself is gone,
    /// so list the keys or the endpoints, revoke or rotate, and try again.
    /// </summary>
    public bool WasReplayed => Response.WasReplayed;
}
