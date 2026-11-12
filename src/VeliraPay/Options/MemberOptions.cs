namespace VeliraPay;

/// <summary>A member's new role.</summary>
public sealed class MemberUpdateOptions : BaseOptions
{
    /// <summary>One of the <see cref="MemberRole"/> values but <see cref="MemberRole.Owner"/>.</summary>
    public required string Role { get => Get<string>("role")!; set => Set("role", value); }
}

/// <summary>A new invitation to the account.</summary>
public sealed class InvitationCreateOptions : BaseOptions
{
    /// <summary>Where to send it.</summary>
    public required string Email { get => Get<string>("email")!; set => Set("email", value); }

    /// <summary>The role it gives once accepted, one of the <see cref="MemberRole"/> values but <see cref="MemberRole.Owner"/>.</summary>
    public required string Role { get => Get<string>("role")!; set => Set("role", value); }
}
