namespace VeliraPay;

/// <summary>How an account's checkout looks, as <see cref="AccountCheckout.Appearance"/> gives it and <see cref="AccountCheckoutOptions.Appearance"/> takes it.</summary>
public static class CheckoutAppearance
{
    /// <summary>Light or dark, following the customer's device.</summary>
    public const string Auto = "auto";

    /// <summary>Always light.</summary>
    public const string Light = "light";

    /// <summary>Always dark.</summary>
    public const string Dark = "dark";
}
