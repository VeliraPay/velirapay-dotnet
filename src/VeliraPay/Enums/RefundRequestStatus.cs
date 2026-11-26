namespace VeliraPay;

/// <summary>The states a refund request moves through, from the customer's address to the refund landing, as <see cref="RefundRequest.Status"/> gives them.</summary>
public static class RefundRequestStatus
{
    /// <summary>The customer has not given the address yet.</summary>
    public const string AwaitingAddress = "awaiting_address";

    /// <summary>There is an address, and the refund waits for you to send it from your own wallet.</summary>
    public const string AwaitingTransfer = "awaiting_transfer";

    /// <summary>The chain shows the transfer, which waits for its confirmations.</summary>
    public const string Confirming = "confirming";

    /// <summary>Recorded as a refund, in <see cref="RefundRequest.Refund"/>.</summary>
    public const string Refunded = "refunded";

    /// <summary>Stopped, which freed what it set aside.</summary>
    public const string Canceled = "canceled";

    /// <summary>The customer never gave the address before the link lapsed.</summary>
    public const string Expired = "expired";
}
