namespace VeliraPay;

/// <summary>The base of every exception this library throws, so one <c>catch</c> can handle them all.</summary>
public abstract class VeliraPayException : Exception
{
    /// <summary>Create a new exception.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The exception that caused it, if any.</param>
    protected VeliraPayException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
