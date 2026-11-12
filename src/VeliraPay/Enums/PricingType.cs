namespace VeliraPay;

/// <summary>How a payment link is priced: at a set amount, or at one the customer names.</summary>
public static class PricingType
{
    /// <summary>A set price.</summary>
    public const string Fixed = "fixed";

    /// <summary>The customer names the amount.</summary>
    public const string Open = "open";
}
