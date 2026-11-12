namespace VeliraPay;

/// <summary>A webhook delivery's signature is missing, does not match, or is too old.</summary>
public sealed class SignatureVerificationException : VeliraPayException
{
    /// <summary>Create a new exception.</summary>
    /// <param name="message">What is wrong with the signature.</param>
    public SignatureVerificationException(string message)
        : base(message)
    {
    }
}
