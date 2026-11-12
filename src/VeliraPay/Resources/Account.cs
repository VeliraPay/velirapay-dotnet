using System.Text.Json;

namespace VeliraPay;

/// <summary>The account an API key belongs to: a personal account or an organization.</summary>
public sealed class Account : ApiResource
{
    /// <summary>Read an account from the JSON the API sent.</summary>
    /// <param name="json">The account, as a JSON object.</param>
    /// <param name="meta">What the answer said about it besides, such as the key's mode and the coins it accepts.</param>
    public Account(JsonElement json, JsonElement? meta = null)
        : base(json)
    {
        var data = RawJson;
        var about = meta?.ValueKind == JsonValueKind.Object ? meta.Value.Clone() : JsonValues.EmptyObject;
        var plan = JsonValues.Object(data, "plan");

        Id = JsonValues.String(data, "id");
        Type = JsonValues.String(data, "type");
        Name = JsonValues.String(data, "name");
        DisplayName = JsonValues.String(data, "display_name", Name);
        LogoUrl = JsonValues.NullableString(data, "logo_url");
        LogoDarkUrl = JsonValues.NullableString(data, "logo_dark_url");
        WebsiteUrl = JsonValues.NullableString(data, "website_url");
        SupportEmail = JsonValues.NullableString(data, "support_email");
        SupportPhone = JsonValues.NullableString(data, "support_phone");
        BrandColor = JsonValues.NullableString(data, "brand_color");
        TermsUrl = JsonValues.NullableString(data, "terms_url");
        RefundPolicyUrl = JsonValues.NullableString(data, "refund_policy_url");
        Business = new AccountBusiness(JsonValues.Object(data, "business"));
        DefaultCurrency = JsonValues.String(data, "default_currency");
        UnderpaymentTolerance = JsonValues.Decimal(data, "underpayment_tolerance");
        OverpaymentThreshold = JsonValues.Decimal(data, "overpayment_threshold");
        Plan = JsonValues.String(plan, "name", "free");
        FeePercent = JsonValues.Decimal(plan, "fee_percent");
        ProEndsAt = JsonValues.Date(plan, "pro_ends_at");
        Checkout = JsonValues.TryGet(data, "checkout", out var checkout) && checkout.ValueKind == JsonValueKind.Object
            ? new AccountCheckout
            {
                Appearance = JsonValues.String(checkout, "appearance", "auto"),
                CoinOrder = JsonValues.Strings(checkout, "coin_order"),
                DefaultCoin = JsonValues.NullableString(checkout, "default_coin"),
                SuccessUrl = JsonValues.NullableString(checkout, "success_url"),
                CancelUrl = JsonValues.NullableString(checkout, "cancel_url"),
                SuccessMessage = JsonValues.NullableString(checkout, "success_message"),
                HidePoweredBy = JsonValues.Bool(checkout, "hide_powered_by"),
            }
            : null;
        Invoices = JsonValues.TryGet(data, "invoices", out var invoices) && invoices.ValueKind == JsonValueKind.Object
            ? new AccountInvoices
            {
                NumberPrefix = JsonValues.NullableString(invoices, "number_prefix"),
                Footer = JsonValues.NullableString(invoices, "footer"),
                ShowBusinessDetails = JsonValues.Bool(invoices, "show_business_details"),
            }
            : null;
        CheckoutLocale = JsonValues.NullableString(data, "checkout_locale");
        InvoiceReminders = JsonValues.Bool(data, "invoice_reminders");
        ApiPayoutAddressChanges = JsonValues.Bool(data, "api_payout_address_changes");
        CreatedAt = JsonValues.Date(data, "created_at");
        Mode = JsonValues.NullableString(about, "mode");
        AcceptedAssets = JsonValues.Strings(about, "accepted_assets");
    }

    /// <summary>The account's slug.</summary>
    public string Id { get; }

    /// <summary>Either "personal" or "organization".</summary>
    public string Type { get; }

    /// <summary>The account's name.</summary>
    public string Name { get; }

    /// <summary>The name customers see.</summary>
    public string DisplayName { get; }

    /// <summary>The logo customers see.</summary>
    public string? LogoUrl { get; }

    /// <summary>The logo customers see on a dark background.</summary>
    public string? LogoDarkUrl { get; }

    /// <summary>The merchant's website.</summary>
    public string? WebsiteUrl { get; }

    /// <summary>Where customers can reach the merchant.</summary>
    public string? SupportEmail { get; }

    /// <summary>The phone number customers see next to the support email.</summary>
    public string? SupportPhone { get; }

    /// <summary>The checkout's accent colour, as "#rrggbb".</summary>
    public string? BrandColor { get; }

    /// <summary>The merchant's terms and conditions.</summary>
    public string? TermsUrl { get; }

    /// <summary>The merchant's refund policy.</summary>
    public string? RefundPolicyUrl { get; }

    /// <summary>Who runs the account and what it sells; every field is null until it is filled in, and for a key without the sensitive account:write permission.</summary>
    public AccountBusiness Business { get; }

    /// <summary>The currency new links and reports default to.</summary>
    public string DefaultCurrency { get; }

    /// <summary>How far short of the amount due, in percent, still counts as paid.</summary>
    public decimal UnderpaymentTolerance { get; }

    /// <summary>How much surplus, in percent, is ignored before a charge is flagged as overpaid.</summary>
    public decimal OverpaymentThreshold { get; }

    /// <summary>"free" or "pro".</summary>
    public string Plan { get; }

    /// <summary>The share of each payment kept as the platform's fee, in percent.</summary>
    public decimal FeePercent { get; }

    /// <summary>When Pro ends, or null while it renews, has no end or the account is on the free plan.</summary>
    public DateTimeOffset? ProEndsAt { get; }

    /// <summary>The checkout's appearance, coin order, default coin and defaults, or null when the answer has none.</summary>
    public AccountCheckout? Checkout { get; }

    /// <summary>The invoice numbering, footer and business details settings, or null when the answer has none.</summary>
    public AccountInvoices? Invoices { get; }

    /// <summary>The fallback checkout language, or null for English.</summary>
    public string? CheckoutLocale { get; }

    /// <summary>Whether automatic invoice reminders are on.</summary>
    public bool InvoiceReminders { get; }

    /// <summary>Whether an owner allows API keys and AI apps to change payout addresses.</summary>
    public bool ApiPayoutAddressChanges { get; }

    /// <summary>When the account was created.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>The mode of the API key that fetched the account: <see cref="VeliraPay.Mode.Live"/> or <see cref="VeliraPay.Mode.Test"/>.</summary>
    public string? Mode { get; }

    /// <summary>The coins the account can be paid in right now, in that mode.</summary>
    public IReadOnlyList<string> AcceptedAssets { get; }

    /// <summary>Whether the account is on Pro, which keeps no fee.</summary>
    public bool IsPro => Plan == "pro";

    /// <summary>Whether this is an organization rather than a personal account.</summary>
    public bool IsOrganization => Type == "organization";

    /// <summary>Whether the key that fetched the account works in test mode.</summary>
    public bool IsTestMode => Mode == VeliraPay.Mode.Test;
}

/// <summary>How an account's checkout looks and what it falls back on.</summary>
public sealed record AccountCheckout
{
    /// <summary>"auto", "light" or "dark".</summary>
    public required string Appearance { get; init; }

    /// <summary>The order coins are listed in.</summary>
    public required IReadOnlyList<string> CoinOrder { get; init; }

    /// <summary>The coin the checkout starts on, or null when the merchant has not picked one.</summary>
    public string? DefaultCoin { get; init; }

    /// <summary>Where customers are sent after paying, for charges and payment links that set none.</summary>
    public string? SuccessUrl { get; init; }

    /// <summary>Where customers are sent when they give up, for charges and payment links that set none.</summary>
    public string? CancelUrl { get; init; }

    /// <summary>What customers are told once they have paid, for charges and payment links that set nothing.</summary>
    public string? SuccessMessage { get; init; }

    /// <summary>Whether "Powered by VeliraPay" is hidden, on Pro.</summary>
    public bool HidePoweredBy { get; init; }
}

/// <summary>How an account's invoices are numbered and printed.</summary>
public sealed record AccountInvoices
{
    /// <summary>The prefix of new invoice numbers.</summary>
    public string? NumberPrefix { get; init; }

    /// <summary>A note printed at the foot of invoices and receipts.</summary>
    public string? Footer { get; init; }

    /// <summary>Whether invoices and receipts print the business details.</summary>
    public bool ShowBusinessDetails { get; init; }
}

/// <summary>Who runs an account and what it sells, as entered under Business details in its settings.</summary>
public sealed class AccountBusiness : ApiResource
{
    /// <summary>Read business details from the JSON the API sent.</summary>
    /// <param name="json">The details, as a JSON object.</param>
    public AccountBusiness(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Type = JsonValues.OneOf(data, "type", BusinessType.Values);
        LegalName = JsonValues.NullableString(data, "legal_name");
        RegistrationNumber = JsonValues.NullableString(data, "registration_number");
        TaxId = JsonValues.NullableString(data, "tax_id");
        Address = new Address(JsonValues.Object(data, "address"));
        Industry = JsonValues.OneOf(data, "industry", VeliraPay.Industry.Values);
        ProductDescription = JsonValues.NullableString(data, "product_description");
        MonthlyVolume = JsonValues.OneOf(data, "monthly_volume", VeliraPay.MonthlyVolume.Values);
        Phone = JsonValues.NullableString(data, "phone");
    }

    /// <summary>One of the <see cref="BusinessType"/> values, or null when unset or a value this library does not know yet, which <see cref="ApiResource.Get"/> still gives.</summary>
    public string? Type { get; }

    /// <summary>The registered name of the business.</summary>
    public string? LegalName { get; }

    /// <summary>The company registration number.</summary>
    public string? RegistrationNumber { get; }

    /// <summary>The tax or VAT number.</summary>
    public string? TaxId { get; }

    /// <summary>The business's address.</summary>
    public Address Address { get; }

    /// <summary>One of the <see cref="VeliraPay.Industry"/> values, or null when unset or a value this library does not know yet.</summary>
    public string? Industry { get; }

    /// <summary>What the account sells, in its own words.</summary>
    public string? ProductDescription { get; }

    /// <summary>One of the <see cref="VeliraPay.MonthlyVolume"/> values, or null when unset or a value this library does not know yet.</summary>
    public string? MonthlyVolume { get; }

    /// <summary>The business's phone number, in international format.</summary>
    public string? Phone { get; }
}

/// <summary>A postal address.</summary>
public sealed class Address : ApiResource
{
    /// <summary>Read an address from the JSON the API sent.</summary>
    /// <param name="json">The address, as a JSON object.</param>
    public Address(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Line1 = JsonValues.NullableString(data, "line1");
        Line2 = JsonValues.NullableString(data, "line2");
        City = JsonValues.NullableString(data, "city");
        PostalCode = JsonValues.NullableString(data, "postal_code");
        State = JsonValues.NullableString(data, "state");
        Country = JsonValues.NullableString(data, "country");
    }

    /// <summary>The street address.</summary>
    public string? Line1 { get; }

    /// <summary>The apartment, suite or building.</summary>
    public string? Line2 { get; }

    /// <summary>The city or town.</summary>
    public string? City { get; }

    /// <summary>The postal code.</summary>
    public string? PostalCode { get; }

    /// <summary>The state, county or region.</summary>
    public string? State { get; }

    /// <summary>The country, as an ISO 3166 two-letter code.</summary>
    public string? Country { get; }
}
