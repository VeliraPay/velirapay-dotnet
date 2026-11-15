using System.Text.Json;

namespace VeliraPay;

/// <summary>A bill, paid through a hosted page in the coin the customer chooses.</summary>
public sealed class Invoice : ApiResource
{
    /// <summary>Read an invoice from the JSON the API sent.</summary>
    /// <param name="json">The invoice, as a JSON object.</param>
    public Invoice(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Id = JsonValues.String(data, "id", JsonValues.String(data, "code"));
        Number = JsonValues.String(data, "number");
        Status = JsonValues.String(data, "status");
        Overdue = JsonValues.NullableBool(data, "overdue");
        CustomerName = JsonValues.NullableString(data, "customer_name");
        CustomerEmail = JsonValues.NullableString(data, "customer_email");
        Memo = JsonValues.NullableString(data, "memo");
        Amount = JsonValues.Decimal(data, "amount");
        Currency = JsonValues.String(data, "currency");
        Items = JsonValues.Objects(data, "items", item => new InvoiceItem(item));
        DueAt = JsonValues.Day(data, "due_at");
        ExpiresAt = JsonValues.Date(data, "expires_at");
        HostedUrl = JsonValues.String(data, "hosted_url");
        DocumentUrl = JsonValues.NullableString(data, "document_url");
        Charges = [.. JsonValues.Records(data, "charges").Select(charge => new InvoiceCharge(JsonValues.String(charge, "id", JsonValues.String(charge, "code")), JsonValues.String(charge, "status"), JsonValues.String(charge, "asset")))];
        SentAt = JsonValues.Date(data, "sent_at");
        ViewedAt = JsonValues.Date(data, "viewed_at");
        PaidAt = JsonValues.Date(data, "paid_at");
        VoidedAt = JsonValues.Date(data, "voided_at");
        CreatedAt = JsonValues.Date(data, "created_at");
        SuccessUrl = JsonValues.NullableString(data, "success_url");
        CancelUrl = JsonValues.NullableString(data, "cancel_url");
        SuccessMessage = JsonValues.NullableString(data, "success_message");
    }

    /// <summary>The invoice's public code, also the last segment of its hosted URL.</summary>
    public string Id { get; }

    /// <summary>The invoice number shown to the customer.</summary>
    public string Number { get; }

    /// <summary>One of the <see cref="InvoiceStatus"/> values, or one the API added after this version of the library.</summary>
    public string Status { get; }

    /// <summary>Whether the invoice is open past its due date; null in a payload that leaves it out, such as an older webhook.</summary>
    public bool? Overdue { get; }

    /// <summary>Who the invoice is addressed to, such as their name or username, or null when only an email was given.</summary>
    public string? CustomerName { get; }

    /// <summary>Where the invoice and the receipt are emailed, or null when only a name was given, and it can then not be sent or reminded.</summary>
    public string? CustomerEmail { get; }

    /// <summary>A note shown on the invoice.</summary>
    public string? Memo { get; }

    /// <summary>The total due.</summary>
    public decimal Amount { get; }

    /// <summary>The fiat currency, such as "USD".</summary>
    public string Currency { get; }

    /// <summary>The lines of an itemised invoice.</summary>
    public IReadOnlyList<InvoiceItem> Items { get; }

    /// <summary>The day the invoice is due.</summary>
    public DateOnly? DueAt { get; }

    /// <summary>When the invoice stops taking new payments, or null when it stays open until it is paid or voided. A payment already started can still be finished; an invoice still unpaid once nothing is under way is voided, with <c>invoice.voided</c>.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>The page where the customer pays.</summary>
    public string HostedUrl { get; }

    /// <summary>The printable invoice; null in a payload that leaves it out, such as an older webhook.</summary>
    public string? DocumentUrl { get; }

    /// <summary>The charges made to pay the invoice, one per coin the customer tried.</summary>
    public IReadOnlyList<InvoiceCharge> Charges { get; }

    /// <summary>When the invoice was emailed.</summary>
    public DateTimeOffset? SentAt { get; }

    /// <summary>When the customer first opened it.</summary>
    public DateTimeOffset? ViewedAt { get; }

    /// <summary>When it was paid.</summary>
    public DateTimeOffset? PaidAt { get; }

    /// <summary>When it was voided.</summary>
    public DateTimeOffset? VoidedAt { get; }

    /// <summary>When it was created.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>The page the customer is offered a button back to once they have paid, or null when the account's default applies.</summary>
    public string? SuccessUrl { get; }

    /// <summary>The page the customer can go back to from the invoice without paying, or null when the account's default applies.</summary>
    public string? CancelUrl { get; }

    /// <summary>What the customer is told once they have paid, or null when the account's own message applies.</summary>
    public string? SuccessMessage { get; }

    /// <summary>Whether the invoice can still be paid.</summary>
    public bool IsOpen => Status == InvoiceStatus.Open;

    /// <summary>Whether the invoice was paid.</summary>
    public bool IsPaid => Status == InvoiceStatus.Paid;

    /// <summary>Whether the invoice was voided.</summary>
    public bool IsVoid => Status == InvoiceStatus.Void;
}

/// <summary>A charge made to pay an invoice, one per coin the customer tried.</summary>
/// <param name="Id">The charge's id.</param>
/// <param name="Status">One of the <see cref="ChargeStatus"/> values.</param>
/// <param name="Asset">The coin the customer tried.</param>
public sealed record InvoiceCharge(string Id, string Status, string Asset);

/// <summary>A line on an itemised invoice.</summary>
public sealed class InvoiceItem : ApiResource
{
    /// <summary>Read an invoice line from the JSON the API sent.</summary>
    /// <param name="json">The line, as a JSON object.</param>
    public InvoiceItem(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Description = JsonValues.String(data, "description");
        Quantity = JsonValues.Decimal(data, "quantity", 1m);
        UnitAmount = JsonValues.Decimal(data, "unit_amount");
        Total = JsonValues.Decimal(data, "total");
    }

    /// <summary>What the line is for.</summary>
    public string Description { get; }

    /// <summary>How many units the line bills.</summary>
    public decimal Quantity { get; }

    /// <summary>The price of one unit, in the invoice's currency.</summary>
    public decimal UnitAmount { get; }

    /// <summary>The quantity times the unit amount.</summary>
    public decimal Total { get; }
}
