namespace VeliraPay;

/// <summary>The kinds of account, as <see cref="Account.Type"/> gives them.</summary>
public static class AccountType
{
    /// <summary>A person's own account.</summary>
    public const string Personal = "personal";

    /// <summary>An organization, which people can be invited to.</summary>
    public const string Organization = "organization";
}
