namespace VeliraPay;

/// <summary>The API could not be reached, or its answer could not be read, after any retries.</summary>
public sealed class ConnectionException : VeliraPayException
{
    /// <summary>Create a new exception.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The network error that caused it.</param>
    public ConnectionException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
