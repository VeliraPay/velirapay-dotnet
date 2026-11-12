namespace VeliraPay;

/// <summary>
/// The languages the checkout is offered in, as <see cref="Account.CheckoutLocale"/> gives them and <see cref="PaymentSettingsUpdateOptions.CheckoutLocale"/> takes them.
/// Customers see the checkout in the language they pick there, or else their browser's when it is one of these, and in the account's fallback otherwise.
/// </summary>
public static class CheckoutLocale
{
    /// <summary>English.</summary>
    public const string English = "en";

    /// <summary>French.</summary>
    public const string French = "fr";

    /// <summary>German.</summary>
    public const string German = "de";

    /// <summary>Spanish.</summary>
    public const string Spanish = "es";

    /// <summary>Turkish.</summary>
    public const string Turkish = "tr";

    /// <summary>Portuguese, as written in Brazil.</summary>
    public const string Portuguese = "pt";

    /// <summary>Russian.</summary>
    public const string Russian = "ru";

    /// <summary>Arabic, written right to left.</summary>
    public const string Arabic = "ar";
}
