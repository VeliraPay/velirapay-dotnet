namespace VeliraPay;

/// <summary>The plans an account can be on, as <see cref="Account.Plan"/> gives them.</summary>
public static class PlanName
{
    /// <summary>The free plan, which keeps a share of each payment as the platform's fee.</summary>
    public const string Free = "free";

    /// <summary>Pro, paid monthly, which keeps no fee.</summary>
    public const string Pro = "pro";
}
