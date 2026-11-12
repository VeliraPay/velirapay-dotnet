namespace VeliraPay;

/// <summary>The library was given something it cannot use, such as an API key that does not start with "vp_live_" or "vp_test_".</summary>
public sealed class InvalidArgumentException : VeliraPayException
{
    /// <summary>Create a new exception.</summary>
    /// <param name="message">What cannot be used, and why.</param>
    /// <param name="innerException">The exception that caused it, if any.</param>
    public InvalidArgumentException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
