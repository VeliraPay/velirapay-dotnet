namespace VeliraPay;

/// <summary>Changes to the account, in the shape <see cref="AccountService.RetrieveAsync"/> reads: only the properties you set are changed, and null clears one.</summary>
public sealed class AccountUpdateOptions : BaseOptions
{
    /// <summary>The account's name.</summary>
    public string? Name { get => Get<string>("name"); set => Set("name", value); }

    /// <summary>The name customers see.</summary>
    public string? DisplayName { get => Get<string>("display_name"); set => Set("display_name", value); }

    /// <summary>The merchant's https:// website.</summary>
    public string? WebsiteUrl { get => Get<string>("website_url"); set => Set("website_url", value); }

    /// <summary>Where customers can reach the merchant.</summary>
    public string? SupportEmail { get => Get<string>("support_email"); set => Set("support_email", value); }

    /// <summary>The phone number customers see next to the support email.</summary>
    public string? SupportPhone { get => Get<string>("support_phone"); set => Set("support_phone", value); }

    /// <summary>The merchant's https:// terms and conditions.</summary>
    public string? TermsUrl { get => Get<string>("terms_url"); set => Set("terms_url", value); }

    /// <summary>The merchant's https:// refund policy.</summary>
    public string? RefundPolicyUrl { get => Get<string>("refund_policy_url"); set => Set("refund_policy_url", value); }

    /// <summary>The checkout's accent colour, as "#rrggbb".</summary>
    public string? BrandColor { get => Get<string>("brand_color"); set => Set("brand_color", value); }

    /// <summary>The checkout's appearance, coin order, default coin and defaults.</summary>
    public AccountCheckoutOptions? Checkout { get => Get<AccountCheckoutOptions>("checkout"); set => Set("checkout", value); }

    /// <summary>The invoice numbering, footer and business details settings.</summary>
    public AccountInvoicesOptions? Invoices { get => Get<AccountInvoicesOptions>("invoices"); set => Set("invoices", value); }

    /// <summary>Who runs the account and what it sells.</summary>
    public AccountBusinessOptions? Business { get => Get<AccountBusinessOptions>("business"); set => Set("business", value); }
}

/// <summary>Changes to how the account's checkout looks and what it falls back on.</summary>
public sealed class AccountCheckoutOptions : BaseOptions
{
    /// <summary>"auto", "light" or "dark".</summary>
    public string? Appearance { get => Get<string>("appearance"); set => Set("appearance", value); }

    /// <summary>The order coins are listed in.</summary>
    public IReadOnlyList<string>? CoinOrder { get => Get<IReadOnlyList<string>>("coin_order"); set => Set("coin_order", value); }

    /// <summary>The coin the checkout starts on.</summary>
    public string? DefaultCoin { get => Get<string>("default_coin"); set => Set("default_coin", value); }

    /// <summary>Where customers are sent after paying, for charges and payment links that set none.</summary>
    public string? SuccessUrl { get => Get<string>("success_url"); set => Set("success_url", value); }

    /// <summary>Where customers are sent when they give up, for charges and payment links that set none.</summary>
    public string? CancelUrl { get => Get<string>("cancel_url"); set => Set("cancel_url", value); }

    /// <summary>What customers are told once they have paid, for charges and payment links that set nothing.</summary>
    public string? SuccessMessage { get => Get<string>("success_message"); set => Set("success_message", value); }

    /// <summary>Whether to hide "Powered by VeliraPay", on Pro.</summary>
    public bool? HidePoweredBy { get => Get<bool?>("hide_powered_by"); set => Set("hide_powered_by", value); }
}

/// <summary>Changes to how the account's invoices are numbered and printed.</summary>
public sealed class AccountInvoicesOptions : BaseOptions
{
    /// <summary>The prefix of new invoice numbers.</summary>
    public string? NumberPrefix { get => Get<string>("number_prefix"); set => Set("number_prefix", value); }

    /// <summary>A note printed at the foot of invoices and receipts.</summary>
    public string? Footer { get => Get<string>("footer"); set => Set("footer", value); }

    /// <summary>Whether invoices and receipts print the business details.</summary>
    public bool? ShowBusinessDetails { get => Get<bool?>("show_business_details"); set => Set("show_business_details", value); }
}

/// <summary>Changes to who runs the account and what it sells.</summary>
public sealed class AccountBusinessOptions : BaseOptions
{
    /// <summary>The kind of legal entity, one of the <see cref="BusinessType"/> values.</summary>
    public string? Type { get => Get<string>("type"); set => Set("type", value); }

    /// <summary>The registered name of the business.</summary>
    public string? LegalName { get => Get<string>("legal_name"); set => Set("legal_name", value); }

    /// <summary>The company registration number.</summary>
    public string? RegistrationNumber { get => Get<string>("registration_number"); set => Set("registration_number", value); }

    /// <summary>The tax or VAT number.</summary>
    public string? TaxId { get => Get<string>("tax_id"); set => Set("tax_id", value); }

    /// <summary>The business's address.</summary>
    public AddressOptions? Address { get => Get<AddressOptions>("address"); set => Set("address", value); }

    /// <summary>What the account sells, one of the <see cref="VeliraPay.Industry"/> values.</summary>
    public string? Industry { get => Get<string>("industry"); set => Set("industry", value); }

    /// <summary>What the account sells, in its own words.</summary>
    public string? ProductDescription { get => Get<string>("product_description"); set => Set("product_description", value); }

    /// <summary>The expected monthly takings, one of the <see cref="VeliraPay.MonthlyVolume"/> values.</summary>
    public string? MonthlyVolume { get => Get<string>("monthly_volume"); set => Set("monthly_volume", value); }

    /// <summary>The business's phone number, in international format.</summary>
    public string? Phone { get => Get<string>("phone"); set => Set("phone", value); }
}

/// <summary>Changes to a postal address.</summary>
public sealed class AddressOptions : BaseOptions
{
    /// <summary>The street address.</summary>
    public string? Line1 { get => Get<string>("line1"); set => Set("line1", value); }

    /// <summary>The apartment, suite or building.</summary>
    public string? Line2 { get => Get<string>("line2"); set => Set("line2", value); }

    /// <summary>The city or town.</summary>
    public string? City { get => Get<string>("city"); set => Set("city", value); }

    /// <summary>The postal code.</summary>
    public string? PostalCode { get => Get<string>("postal_code"); set => Set("postal_code", value); }

    /// <summary>The state, county or region.</summary>
    public string? State { get => Get<string>("state"); set => Set("state", value); }

    /// <summary>The country, as an ISO 3166 two-letter code.</summary>
    public string? Country { get => Get<string>("country"); set => Set("country", value); }
}

/// <summary>Changes to the payment settings: only the properties you set are changed.</summary>
public sealed class PaymentSettingsUpdateOptions : BaseOptions
{
    /// <summary>The currency new links and reports default to.</summary>
    public string? DefaultCurrency { get => Get<string>("default_currency"); set => Set("default_currency", value); }

    /// <summary>How far short of the amount due, in percent with at most 2 decimal places, still counts as paid.</summary>
    public decimal? UnderpaymentTolerance { get => Get<decimal?>("underpayment_tolerance"); set => Set("underpayment_tolerance", value); }

    /// <summary>How much surplus, in percent with at most 2 decimal places, is ignored before a charge is flagged as overpaid.</summary>
    public decimal? OverpaymentThreshold { get => Get<decimal?>("overpayment_threshold"); set => Set("overpayment_threshold", value); }

    /// <summary>The fallback checkout language: "en", "fr", "de", "es", "tr", "pt", "ru" or "ar"; null for English.</summary>
    public string? CheckoutLocale { get => Get<string>("checkout_locale"); set => Set("checkout_locale", value); }

    /// <summary>Whether open invoices are reminded automatically.</summary>
    public bool? InvoiceReminders { get => Get<bool?>("invoice_reminders"); set => Set("invoice_reminders", value); }
}
