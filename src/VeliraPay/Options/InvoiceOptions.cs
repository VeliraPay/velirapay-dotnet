namespace VeliraPay;

/// <summary>A new invoice: a bill for one customer, for a total or a list of items, paid on a hosted page in the coin they choose.</summary>
public sealed class InvoiceCreateOptions : BaseOptions
{
    /// <summary>The customer's name.</summary>
    public required string CustomerName { get => Get<string>("customer_name")!; set => Set("customer_name", value); }

    /// <summary>The customer's email address, where the invoice is sent.</summary>
    public required string CustomerEmail { get => Get<string>("customer_email")!; set => Set("customer_email", value); }

    /// <summary>The fiat currency, such as <see cref="VeliraPay.Currency.USD"/>.</summary>
    public required string Currency { get => Get<string>("currency")!; set => Set("currency", value); }

    /// <summary>A single total, instead of <see cref="Items"/>.</summary>
    public decimal? Amount { get => Get<decimal?>("amount"); set => Set("amount", value); }

    /// <summary>Up to 50 lines, which make up the total when given.</summary>
    public IReadOnlyList<InvoiceItemOptions>? Items { get => Get<IReadOnlyList<InvoiceItemOptions>>("items"); set => Set("items", value); }

    /// <summary>A note shown on the invoice.</summary>
    public string? Memo { get => Get<string>("memo"); set => Set("memo", value); }

    /// <summary>The day it is due: today or later.</summary>
    public DateOnly? DueAt { get => Get<DateOnly?>("due_at"); set => Set("due_at", value); }

    /// <summary>Whether to email it to the customer straight away.</summary>
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
