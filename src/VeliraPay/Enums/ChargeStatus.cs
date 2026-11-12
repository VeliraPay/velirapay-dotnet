namespace VeliraPay;

/// <summary>The states a charge moves through, as <see cref="Charge.Status"/> gives them.</summary>
public static class ChargeStatus
{
    /// <summary>Waiting for its payment.</summary>
    public const string Pending = "pending";

    /// <summary>Less than the amount due arrived.</summary>
    public const string Underpaid = "underpaid";

    /// <summary>Paid in full and confirmed.</summary>
    public const string Paid = "paid";

    /// <summary>The locked rate ran out before the charge was paid.</summary>
    public const string Expired = "expired";

    /// <summary>Canceled, so it can no longer be paid.</summary>
    public const string Canceled = "canceled";
}
