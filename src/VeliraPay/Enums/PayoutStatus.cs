namespace VeliraPay;

/// <summary>Where a payout has got to, as <see cref="Payout.Status"/> gives it.</summary>
public static class PayoutStatus
{
    /// <summary>Waiting to go out.</summary>
    public const string Pending = "pending";

    /// <summary>Broadcast, and waiting for the chain to confirm it.</summary>
    public const string Sending = "sending";

    /// <summary>Confirmed, so the money is in the merchant's wallet.</summary>
    public const string Sent = "sent";

    /// <summary>Every attempt to send it failed; an operator puts it back in the queue.</summary>
    public const string Failed = "failed";

    /// <summary>Waiting for a payout address, or for a person.</summary>
    public const string Manual = "manual";
}
