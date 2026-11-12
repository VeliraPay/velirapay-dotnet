namespace VeliraPay;

/// <summary>A new payment link: one reusable public page many customers pay through, never one per customer.</summary>
public sealed class PaymentLinkCreateOptions : BaseOptions
{
    /// <summary>The title shown on the checkout.</summary>
    public required string Title { get => Get<string>("title")!; set => Set("title", value); }

    /// <summary>How the link is priced, one of the <see cref="VeliraPay.PricingType"/> values.</summary>
    public required string PricingType { get => Get<string>("pricing_type")!; set => Set("pricing_type", value); }

    /// <summary>The fiat currency the link is priced in.</summary>
    public required string Currency { get => Get<string>("currency")!; set => Set("currency", value); }

    /// <summary>The price of a fixed-price link.</summary>
    public decimal? Amount { get => Get<decimal?>("amount"); set => Set("amount", value); }

    /// <summary>The description shown on the checkout.</summary>
    public string? Description { get => Get<string>("description"); set => Set("description", value); }

    /// <summary>Up to 4 amounts offered as shortcuts on an open-price link.</summary>
    public IReadOnlyList<decimal>? SuggestedAmounts { get => Get<IReadOnlyList<decimal>>("suggested_amounts"); set => Set("suggested_amounts", value); }

    /// <summary>The least a customer may pay on an open-price link.</summary>
    public decimal? MinAmount { get => Get<decimal?>("min_amount"); set => Set("min_amount", value); }

    /// <summary>The most a customer may pay on an open-price link.</summary>
    public decimal? MaxAmount { get => Get<decimal?>("max_amount"); set => Set("max_amount", value); }

    /// <summary>The coins the link takes; null for every coin the account takes.</summary>
    public IReadOnlyList<string>? AcceptedAssets { get => Get<IReadOnlyList<string>>("accepted_assets"); set => Set("accepted_assets", value); }

    /// <summary>The https:// page the customer is sent to after paying.</summary>
    public string? SuccessUrl { get => Get<string>("success_url"); set => Set("success_url", value); }

    /// <summary>The https:// page the customer is sent to when they give up.</summary>
    public string? CancelUrl { get => Get<string>("cancel_url"); set => Set("cancel_url", value); }

    /// <summary>Whether the checkout asks for the customer's name.</summary>
    public bool? CollectName { get => Get<bool?>("collect_name"); set => Set("collect_name", value); }

    /// <summary>Whether the checkout requires the customer's email address.</summary>
    public bool? RequireEmail { get => Get<bool?>("require_email"); set => Set("require_email", value); }

    /// <summary>The message shown once the payment lands.</summary>
    public string? SuccessMessage { get => Get<string>("success_message"); set => Set("success_message", value); }

    /// <summary>Up to 5 questions the checkout asks.</summary>
    public IReadOnlyList<CheckoutQuestionOptions>? CustomFields { get => Get<IReadOnlyList<CheckoutQuestionOptions>>("custom_fields"); set => Set("custom_fields", value); }

    /// <summary>When the link stops taking payments.</summary>
    public DateTimeOffset? ExpiresAt { get => Get<DateTimeOffset?>("expires_at"); set => Set("expires_at", value); }

    /// <summary>How many payments the link takes before it closes.</summary>
    public int? MaxPayments { get => Get<int?>("max_payments"); set => Set("max_payments", value); }
}

/// <summary>Changes to a payment link: only the properties you set are changed, and null clears one.</summary>
public sealed class PaymentLinkUpdateOptions : BaseOptions
{
    /// <summary>The title shown on the checkout.</summary>
    public string? Title { get => Get<string>("title"); set => Set("title", value); }

    /// <summary>How the link is priced, one of the <see cref="VeliraPay.PricingType"/> values.</summary>
    public string? PricingType { get => Get<string>("pricing_type"); set => Set("pricing_type", value); }

    /// <summary>The fiat currency the link is priced in.</summary>
    public string? Currency { get => Get<string>("currency"); set => Set("currency", value); }

    /// <summary>The price of a fixed-price link.</summary>
    public decimal? Amount { get => Get<decimal?>("amount"); set => Set("amount", value); }

    /// <summary>The description shown on the checkout.</summary>
    public string? Description { get => Get<string>("description"); set => Set("description", value); }

    /// <summary>Up to 4 amounts offered as shortcuts on an open-price link.</summary>
    public IReadOnlyList<decimal>? SuggestedAmounts { get => Get<IReadOnlyList<decimal>>("suggested_amounts"); set => Set("suggested_amounts", value); }

    /// <summary>The least a customer may pay on an open-price link.</summary>
    public decimal? MinAmount { get => Get<decimal?>("min_amount"); set => Set("min_amount", value); }

    /// <summary>The most a customer may pay on an open-price link.</summary>
    public decimal? MaxAmount { get => Get<decimal?>("max_amount"); set => Set("max_amount", value); }

    /// <summary>The coins the link takes; null for every coin the account takes.</summary>
    public IReadOnlyList<string>? AcceptedAssets { get => Get<IReadOnlyList<string>>("accepted_assets"); set => Set("accepted_assets", value); }

    /// <summary>The https:// page the customer is sent to after paying.</summary>
    public string? SuccessUrl { get => Get<string>("success_url"); set => Set("success_url", value); }

    /// <summary>The https:// page the customer is sent to when they give up.</summary>
    public string? CancelUrl { get => Get<string>("cancel_url"); set => Set("cancel_url", value); }

    /// <summary>Whether the checkout asks for the customer's name.</summary>
    public bool? CollectName { get => Get<bool?>("collect_name"); set => Set("collect_name", value); }

    /// <summary>Whether the checkout requires the customer's email address.</summary>
    public bool? RequireEmail { get => Get<bool?>("require_email"); set => Set("require_email", value); }

    /// <summary>The message shown once the payment lands.</summary>
    public string? SuccessMessage { get => Get<string>("success_message"); set => Set("success_message", value); }

    /// <summary>Up to 5 questions the checkout asks.</summary>
    public IReadOnlyList<CheckoutQuestionOptions>? CustomFields { get => Get<IReadOnlyList<CheckoutQuestionOptions>>("custom_fields"); set => Set("custom_fields", value); }

    /// <summary>When the link stops taking payments.</summary>
    public DateTimeOffset? ExpiresAt { get => Get<DateTimeOffset?>("expires_at"); set => Set("expires_at", value); }

    /// <summary>How many payments the link takes before it closes.</summary>
    public int? MaxPayments { get => Get<int?>("max_payments"); set => Set("max_payments", value); }
}

/// <summary>A question a payment link's checkout asks.</summary>
public sealed class CheckoutQuestionOptions : BaseOptions
{
    /// <summary>The question, up to 60 characters.</summary>
    public required string Label { get => Get<string>("label")!; set => Set("label", value); }

    /// <summary>Whether the customer must answer it.</summary>
    public required bool Required { get => Get<bool?>("required") ?? false; set => Set("required", value); }
}

/// <summary>Which payment links to list.</summary>
public sealed class PaymentLinkListOptions : ListOptions
{
    /// <summary>Only links in this state, one of the <see cref="PaymentLinkStatus"/> values.</summary>
    public string? Status { get => Get<string>("status"); set => Set("status", value); }

    /// <summary>Part of a link's title.</summary>
    public string? Search { get => Get<string>("search"); set => Set("search", value); }
}
