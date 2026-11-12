namespace VeliraPay;

/// <summary>Whether a payment link takes payments, as <see cref="PaymentLink.Status"/> gives it.</summary>
public static class PaymentLinkStatus
{
    /// <summary>Taking payments.</summary>
    public const string Active = "active";

    /// <summary>Archived, so it takes no payments until it is restored.</summary>
    public const string Archived = "archived";
}
