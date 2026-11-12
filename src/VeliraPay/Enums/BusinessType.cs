namespace VeliraPay;

/// <summary>What kind of legal entity runs an account.</summary>
public static class BusinessType
{
    /// <summary>A person trading in their own name.</summary>
    public const string Individual = "individual";

    /// <summary>A company.</summary>
    public const string Company = "company";

    /// <summary>A non-profit organization.</summary>
    public const string NonProfit = "non_profit";

    /// <summary>Every value this version of the library knows.</summary>
    internal static readonly string[] Values = [Individual, Company, NonProfit];
}
