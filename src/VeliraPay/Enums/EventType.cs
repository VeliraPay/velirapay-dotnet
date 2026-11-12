namespace VeliraPay;

/// <summary>The events a charge, an invoice or a payout reports, through webhooks and the events API.</summary>
public static class EventType
{
    /// <summary>A charge was created.</summary>
    public const string ChargeCreated = "charge.created";

    /// <summary>A transfer to a charge was seen, before it is confirmed: tell the customer, but do not fulfil.</summary>
    public const string ChargePaymentDetected = "charge.payment_detected";

    /// <summary>A charge is paid in full and confirmed.</summary>
    public const string ChargePaid = "charge.paid";

    /// <summary>Less than the amount due arrived.</summary>
    public const string ChargeUnderpaid = "charge.underpaid";

    /// <summary>
    /// A transfer arrived after the charge expired or was canceled, or topped up an underpaid one after its rate lock ended, and waits for the merchant to accept it;
    /// or it arrived on top of a charge already paid, and was counted automatically.
    /// </summary>
    public const string ChargeLatePayment = "charge.late_payment";

    /// <summary>A refund was recorded against a charge.</summary>
    public const string ChargeRefunded = "charge.refunded";

    /// <summary>A charge's locked rate ran out before it was paid.</summary>
    public const string ChargeExpired = "charge.expired";

    /// <summary>A charge was canceled.</summary>
    public const string ChargeCanceled = "charge.canceled";

    /// <summary>An invoice was created.</summary>
    public const string InvoiceCreated = "invoice.created";

    /// <summary>An invoice was emailed to its customer.</summary>
    public const string InvoiceSent = "invoice.sent";

    /// <summary>The customer opened an invoice for the first time.</summary>
    public const string InvoiceViewed = "invoice.viewed";

    /// <summary>An invoice was paid.</summary>
    public const string InvoicePaid = "invoice.paid";

    /// <summary>An invoice was voided.</summary>
    public const string InvoiceVoided = "invoice.voided";

    /// <summary>A charge's payout was broadcast, or broadcast again.</summary>
    public const string PayoutSent = "payout.sent";

    /// <summary>A charge's payout confirmed on its chain.</summary>
    public const string PayoutConfirmed = "payout.confirmed";
}
