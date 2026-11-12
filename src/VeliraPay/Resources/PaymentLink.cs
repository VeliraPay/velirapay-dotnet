using System.Text.Json;

namespace VeliraPay;

/// <summary>A shareable checkout page that creates a charge for each customer who pays through it.</summary>
public sealed class PaymentLink : ApiResource
{
    /// <summary>Read a payment link from the JSON the API sent.</summary>
    /// <param name="json">The payment link, as a JSON object.</param>
    public PaymentLink(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Id = JsonValues.String(data, "id", JsonValues.String(data, "code"));
        Title = JsonValues.String(data, "title");
        Description = JsonValues.NullableString(data, "description");
        PricingType = JsonValues.String(data, "pricing_type");
        Amount = JsonValues.NullableDecimal(data, "amount");
        SuggestedAmounts = JsonValues.Amounts(data, "suggested_amounts");
        MinAmount = JsonValues.NullableDecimal(data, "min_amount");
        MaxAmount = JsonValues.NullableDecimal(data, "max_amount");
        Currency = JsonValues.String(data, "currency");
        AcceptedAssets = JsonValues.NullableStrings(data, "accepted_assets");
        Status = JsonValues.String(data, "status");
        SuccessUrl = JsonValues.NullableString(data, "success_url");
        CancelUrl = JsonValues.NullableString(data, "cancel_url");
        CollectName = JsonValues.Bool(data, "collect_name");
        RequireEmail = JsonValues.Bool(data, "require_email");
        CustomFields = [.. JsonValues.Records(data, "custom_fields").Select(field => new CheckoutQuestion(JsonValues.String(field, "label"), JsonValues.Bool(field, "required")))];
        SuccessMessage = JsonValues.NullableString(data, "success_message");
        ExpiresAt = JsonValues.Date(data, "expires_at");
        MaxPayments = JsonValues.NullableInt(data, "max_payments");
        AcceptingPayments = JsonValues.Bool(data, "accepting_payments");
        ViewsCount = JsonValues.Int(data, "views_count");
        CheckoutUrl = JsonValues.String(data, "checkout_url");
        CreatedAt = JsonValues.Date(data, "created_at");
        UpdatedAt = JsonValues.Date(data, "updated_at");
    }

    /// <summary>The link's public code, also the last segment of its checkout URL.</summary>
    public string Id { get; }

    /// <summary>The title shown on the checkout.</summary>
    public string Title { get; }

    /// <summary>The description shown on the checkout.</summary>
    public string? Description { get; }

    /// <summary>One of the <see cref="VeliraPay.PricingType"/> values: fixed or open.</summary>
    public string PricingType { get; }

    /// <summary>The price of a fixed-price link.</summary>
    public decimal? Amount { get; }

    /// <summary>Amounts offered as shortcuts on an open-price link.</summary>
    public IReadOnlyList<decimal> SuggestedAmounts { get; }

    /// <summary>The least a customer may pay on an open-price link.</summary>
    public decimal? MinAmount { get; }

    /// <summary>The most a customer may pay on an open-price link.</summary>
    public decimal? MaxAmount { get; }

    /// <summary>The fiat currency the link is priced in.</summary>
    public string Currency { get; }

    /// <summary>The coins the link takes, or null for every coin the account can receive.</summary>
    public IReadOnlyList<string>? AcceptedAssets { get; }

    /// <summary>One of the <see cref="PaymentLinkStatus"/> values: active or archived.</summary>
    public string Status { get; }

    /// <summary>Where the customer is sent after paying.</summary>
    public string? SuccessUrl { get; }

    /// <summary>Where the customer is sent when they give up.</summary>
    public string? CancelUrl { get; }

    /// <summary>Whether the checkout asks for the customer's name.</summary>
    public bool CollectName { get; }

    /// <summary>Whether the checkout requires the customer's email address.</summary>
    public bool RequireEmail { get; }

    /// <summary>The questions the checkout asks.</summary>
    public IReadOnlyList<CheckoutQuestion> CustomFields { get; }

    /// <summary>The message shown once the payment lands.</summary>
    public string? SuccessMessage { get; }

    /// <summary>When the link stops taking payments.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>How many payments the link takes before it closes.</summary>
    public int? MaxPayments { get; }

    /// <summary>Whether the link takes payments right now.</summary>
    public bool AcceptingPayments { get; }

    /// <summary>How many times the checkout was opened.</summary>
    public int ViewsCount { get; }

    /// <summary>The checkout page to share.</summary>
    public string CheckoutUrl { get; }

    /// <summary>When the link was created.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>When the link was last changed.</summary>
    public DateTimeOffset? UpdatedAt { get; }

    /// <summary>Whether the link is active rather than archived.</summary>
    public bool IsActive => Status == PaymentLinkStatus.Active;

    /// <summary>Whether the link was archived.</summary>
    public bool IsArchived => Status == PaymentLinkStatus.Archived;

    /// <summary>Whether the link has a set price rather than one the customer names.</summary>
    public bool HasFixedAmount => PricingType == VeliraPay.PricingType.Fixed;
}

/// <summary>A question a payment link's checkout asks.</summary>
/// <param name="Label">The question.</param>
/// <param name="Required">Whether the customer must answer it.</param>
public sealed record CheckoutQuestion(string Label, bool Required);
