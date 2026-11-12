namespace VeliraPay;

/// <summary>A new charge: how to ask one customer for one payment, in one coin.</summary>
public sealed class ChargeCreateOptions : BaseOptions
{
    /// <summary>The coin and network to pay with, such as <see cref="VeliraPay.Asset.BTC"/>; <see cref="AssetService.ListAsync"/> gives the ones on offer.</summary>
    public required string Asset { get => Get<string>("asset")!; set => Set("asset", value); }

    /// <summary>What the customer owes, in <see cref="Currency"/>. Required unless a payment link with a fixed price is given.</summary>
    public decimal? Amount { get => Get<decimal?>("amount"); set => Set("amount", value); }

    /// <summary>The fiat currency, such as <see cref="VeliraPay.Currency.EUR"/>. Required unless a payment link is given.</summary>
    public string? Currency { get => Get<string>("currency"); set => Set("currency", value); }

    /// <summary>A payment link's id: the currency comes from the link, and so does the price when it is fixed.</summary>
    public string? PaymentLink { get => Get<string>("payment_link"); set => Set("payment_link", value); }

    /// <summary>What you already know about the customer, which helps you spot fraud.</summary>
    public CustomerOptions? Customer { get => Get<CustomerOptions>("customer"); set => Set("customer", value); }

    /// <summary>The customer's email address, the older way to give it; <see cref="Customer"/> is preferred.</summary>
    public string? CustomerEmail { get => Get<string>("customer_email"); set => Set("customer_email", value); }

    /// <summary>A coupon's code, in any case.</summary>
    public string? Coupon { get => Get<string>("coupon"); set => Set("coupon", value); }

    /// <summary>What the charge is for, up to 500 characters.</summary>
    public string? Description { get => Get<string>("description"); set => Set("description", value); }

    /// <summary>Your own key-value data: up to 20 keys of at most 40 characters, each a string of at most 500 characters, a number, a boolean or null.</summary>
    public Dictionary<string, object?>? Metadata { get => Get<Dictionary<string, object?>>("metadata"); set => Set("metadata", value); }

    /// <summary>Up to 5 labelled details shown with the charge.</summary>
    public IReadOnlyList<CustomFieldOptions>? CustomFields { get => Get<IReadOnlyList<CustomFieldOptions>>("custom_fields"); set => Set("custom_fields", value); }

    /// <summary>The https:// page the customer is offered a button back to once they have paid.</summary>
    public string? SuccessUrl { get => Get<string>("success_url"); set => Set("success_url", value); }

    /// <summary>The https:// page the customer can go back to from the checkout without paying.</summary>
    public string? CancelUrl { get => Get<string>("cancel_url"); set => Set("cancel_url", value); }

    /// <summary>What the customer is told once they have paid.</summary>
    public string? SuccessMessage { get => Get<string>("success_message"); set => Set("success_message", value); }

    /// <summary>Whether to email the customer the payment page straight away, which needs their email address.</summary>
    public bool? SendEmail { get => Get<bool?>("send_email"); set => Set("send_email", value); }
}

/// <summary>What you know about the customer paying a charge. A charge from your server comes from your server, so give their own address and browser here.</summary>
public sealed class CustomerOptions : BaseOptions
{
    /// <summary>Their email address.</summary>
    public string? Email { get => Get<string>("email"); set => Set("email", value); }

    /// <summary>Their name.</summary>
    public string? Name { get => Get<string>("name"); set => Set("name", value); }

    /// <summary>The customer's own IPv4 or IPv6 address, not your server's.</summary>
    public string? IpAddress { get => Get<string>("ip_address"); set => Set("ip_address", value); }

    /// <summary>The user agent of the customer's browser.</summary>
    public string? UserAgent { get => Get<string>("user_agent"); set => Set("user_agent", value); }

    /// <summary>Your own id for the customer.</summary>
    public string? Reference { get => Get<string>("reference"); set => Set("reference", value); }

    /// <summary>Their phone number.</summary>
    public string? Phone { get => Get<string>("phone"); set => Set("phone", value); }

    /// <summary>Their country, as an ISO 3166 two-letter code.</summary>
    public string? Country { get => Get<string>("country"); set => Set("country", value); }

    /// <summary>Anything else about the customer, with the same limits as a charge's metadata.</summary>
    public Dictionary<string, object?>? Metadata { get => Get<Dictionary<string, object?>>("metadata"); set => Set("metadata", value); }
}

/// <summary>A labelled detail given when creating a charge.</summary>
public sealed class CustomFieldOptions : BaseOptions
{
    /// <summary>What the detail is, such as "Company".</summary>
    public required string Label { get => Get<string>("label")!; set => Set("label", value); }

    /// <summary>The detail itself.</summary>
    public required string Value { get => Get<string>("value")!; set => Set("value", value); }
}

/// <summary>Which charges to list.</summary>
public sealed class ChargeListOptions : ListOptions
{
    /// <summary>Only charges in this state, one of the <see cref="ChargeStatus"/> values.</summary>
    public string? Status { get => Get<string>("status"); set => Set("status", value); }

    /// <summary>Only charges in this coin.</summary>
    public string? Asset { get => Get<string>("asset"); set => Set("asset", value); }

    /// <summary>Only charges made through this payment link, by its id.</summary>
    public string? PaymentLink { get => Get<string>("payment_link"); set => Set("payment_link", value); }

    /// <summary>
    /// Part of a charge's code, customer email, customer name or description, or exactly its deposit address,
    /// the hash of a transfer to it, its payout hash or the customer's IP address.
    /// </summary>
    public string? Search { get => Get<string>("search"); set => Set("search", value); }

    /// <summary>Only charges created at or after this moment.</summary>
    public DateTimeOffset? CreatedFrom { get => Get<DateTimeOffset?>("created_from"); set => Set("created_from", value); }

    /// <summary>Only charges created at or before this moment.</summary>
    public DateTimeOffset? CreatedTo { get => Get<DateTimeOffset?>("created_to"); set => Set("created_to", value); }
}

/// <summary>A refund you sent the customer from your own wallet, to record against a charge.</summary>
public sealed class RefundOptions : BaseOptions
{
    /// <summary>How much was sent back, in the charge's coin.</summary>
    public required decimal Amount { get => Get<decimal?>("amount") ?? 0m; set => Set("amount", value); }

    /// <summary>The hash of the transaction that carried it.</summary>
    public string? Txid { get => Get<string>("txid"); set => Set("txid", value); }

    /// <summary>Why it was made.</summary>
    public string? Reason { get => Get<string>("reason"); set => Set("reason", value); }
}

/// <summary>How to pay a test-mode charge without coins.</summary>
public sealed class ChargeSimulateOptions : BaseOptions
{
    /// <summary>How much arrives, in the charge's coin; the whole amount due when left out. Less than is due tries an underpayment.</summary>
    public decimal? Amount { get => Get<decimal?>("amount"); set => Set("amount", value); }
}
