namespace VeliraPay;

/// <summary>How much an account expects to take in a month, in US dollars.</summary>
public static class MonthlyVolume
{
    /// <summary>Under $1,000.</summary>
    public const string Under1k = "under_1k";

    /// <summary>$1,000 to $10,000.</summary>
    public const string Under10k = "1k_10k";

    /// <summary>$10,000 to $50,000.</summary>
    public const string Under50k = "10k_50k";

    /// <summary>$50,000 to $250,000.</summary>
    public const string Under250k = "50k_250k";

    /// <summary>Over $250,000.</summary>
    public const string Over250k = "over_250k";

    /// <summary>Every value this version of the library knows.</summary>
    internal static readonly string[] Values = [Under1k, Under10k, Under50k, Under250k, Over250k];
}
