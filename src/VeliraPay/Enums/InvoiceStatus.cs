namespace VeliraPay;

/// <summary>The states an invoice moves through, as <see cref="Invoice.Status"/> gives them.</summary>
public static class InvoiceStatus
{
    /// <summary>Waiting to be paid.</summary>
    public const string Open = "open";

    /// <summary>Paid.</summary>
    public const string Paid = "paid";

    /// <summary>Voided, so it can no longer be paid.</summary>
    public const string Void = "void";
}
