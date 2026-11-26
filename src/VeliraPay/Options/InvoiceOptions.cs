namespace VeliraPay;

/// <summary>A new invoice: a bill for a total or a list of items, paid on a hosted page in the coin the customer chooses.</summary>
public sealed class InvoiceCreateOptions : BaseOptions
{
    /// <summary>Who is billed, such as their name or their username on your site; give this, <see cref="CustomerEmail"/> or both, or the invoice is refused with a <see cref="ValidationException"/> under customer_name.</summary>
    public string? CustomerName { get => Get<string>("customer_name"); set => Set("customer_name", value); }

    /// <summary>Where the invoice and the receipt are emailed; give this, <see cref="CustomerName"/> or both. Required with <see cref="SendEmail"/>, and an invoice without one cannot be sent or reminded.</summary>
    public string? CustomerEmail { get => Get<string>("customer_email"); set => Set("customer_email", value); }

    /// <summary>The fiat currency, such as <see cref="VeliraPay.Currency.USD"/>.</summary>
    public required string Currency { get => Get<string>("currency")!; set => Set("currency", value); }

    /// <summary>A single total, instead of <see cref="Items"/>.</summary>
    public decimal? Amount { get => Get<decimal?>("amount"); set => Set("amount", value); }

    /// <summary>Up to 50 lines, which make up the total when given, less any discount and with any tax.</summary>
    public IReadOnlyList<InvoiceItemOptions>? Items { get => Get<IReadOnlyList<InvoiceItemOptions>>("items"); set => Set("items", value); }

    /// <summary>A percentage taken off the items before tax, above 0 and below 100, with at most 2 decimal places. Only with <see cref="Items"/>, and not with <see cref="DiscountAmount"/>.</summary>
    public decimal? DiscountPercent { get => Get<decimal?>("discount_percent"); set => Set("discount_percent", value); }

    /// <summary>A fixed amount taken off the items before tax, in <see cref="Currency"/>, less than they come to, with at most 2 decimal places. Only with <see cref="Items"/>, and not with <see cref="DiscountPercent"/>.</summary>
    public decimal? DiscountAmount { get => Get<decimal?>("discount_amount"); set => Set("discount_amount", value); }

    /// <summary>A note shown on the invoice.</summary>
    public string? Memo { get => Get<string>("memo"); set => Set("memo", value); }

    /// <summary>The day it is due: today or later.</summary>
    public DateOnly? DueAt { get => Get<DateOnly?>("due_at"); set => Set("due_at", value); }

    /// <summary>When the invoice stops taking new payments: a payment already started can still be finished, and an invoice still unpaid once nothing is under way is voided.</summary>
    public DateTimeOffset? ExpiresAt { get => Get<DateTimeOffset?>("expires_at"); set => Set("expires_at", value); }

    /// <summary>The https:// page the customer is offered a button back to once they have paid.</summary>
    public string? SuccessUrl { get => Get<string>("success_url"); set => Set("success_url", value); }

    /// <summary>The https:// page the customer can go back to from the invoice without paying.</summary>
    public string? CancelUrl { get => Get<string>("cancel_url"); set => Set("cancel_url", value); }

    /// <summary>What the customer is told once they have paid, up to 1000 characters; left out, the account's own message applies.</summary>
    public string? SuccessMessage { get => Get<string>("success_message"); set => Set("success_message", value); }

    /// <summary>Whether to email it to the customer straight away, which needs <see cref="CustomerEmail"/>.</summary>
    public bool? SendEmail { get => Get<bool?>("send_email"); set => Set("send_email", value); }
}

/// <summary>A line on an itemised invoice.</summary>
public sealed class InvoiceItemOptions : BaseOptions
{
    /// <summary>What the line is for.</summary>
    public required string Description { get => Get<string>("description")!; set => Set("description", value); }

    /// <summary>How many units the line bills, from 0.001, with at most 3 decimal places.</summary>
    public required decimal Quantity { get => Get<decimal?>("quantity") ?? 0m; set => Set("quantity", value); }

    /// <summary>The price of one unit, in the invoice's currency, with at most 2 decimal places.</summary>
    public required decimal UnitAmount { get => Get<decimal?>("unit_amount") ?? 0m; set => Set("unit_amount", value); }

    /// <summary>The tax on the line, in percent from 0 to 100 with at most 2 decimal places, added on top of its price after any discount.</summary>
    public decimal? TaxRate { get => Get<decimal?>("tax_rate"); set => Set("tax_rate", value); }
}

/// <summary>Which invoices to list.</summary>
public sealed class InvoiceListOptions : ListOptions
{
    /// <summary>Only invoices in this state, one of the <see cref="InvoiceStatus"/> values.</summary>
    public string? Status { get => Get<string>("status"); set => Set("status", value); }

    /// <summary>Part of an invoice's number, customer name or customer email.</summary>
    public string? Search { get => Get<string>("search"); set => Set("search", value); }

    /// <summary>Only open invoices past their due date.</summary>
    public bool? Overdue { get => Get<bool?>("overdue"); set => Set("overdue", value); }
}
