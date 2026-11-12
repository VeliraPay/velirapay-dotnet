namespace VeliraPay;

/// <summary>The roles a person can have in an account.</summary>
public static class MemberRole
{
    /// <summary>Runs everything. Nobody is made an owner, and no owner is changed or removed, through the API.</summary>
    public const string Owner = "owner";

    /// <summary>Runs everything but handing over the organization.</summary>
    public const string Admin = "admin";

    /// <summary>Creates charges, invoices, payment links and coupons, accepts short and late payments, records refunds and sees payouts.</summary>
    public const string Finance = "finance";

    /// <summary>Looks up charges and customers, emails payment pages and invoices again and cancels charges.</summary>
    public const string Support = "support";

    /// <summary>Sees payments and payouts and changes nothing.</summary>
    public const string Viewer = "viewer";
}
