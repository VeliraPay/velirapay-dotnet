using System.Text.Json;

namespace VeliraPay;

/// <summary>A person in the account, with their role.</summary>
public sealed class Member : ApiResource
{
    /// <summary>Read a member from the JSON the API sent.</summary>
    /// <param name="json">The member, as a JSON object.</param>
    public Member(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Id = JsonValues.String(data, "id");
        Name = JsonValues.String(data, "name");
        Email = JsonValues.String(data, "email");
        Role = JsonValues.NullableString(data, "role");
        JoinedAt = JsonValues.Date(data, "joined_at");
    }

    /// <summary>The member's id.</summary>
    public string Id { get; }

    /// <summary>Their name.</summary>
    public string Name { get; }

    /// <summary>The address they sign in with.</summary>
    public string Email { get; }

    /// <summary>One of the <see cref="MemberRole"/> values, or null when the answer gives none.</summary>
    public string? Role { get; }

    /// <summary>When they joined.</summary>
    public DateTimeOffset? JoinedAt { get; }
}

/// <summary>An invitation to the account nobody has accepted yet.</summary>
public sealed class Invitation : ApiResource
{
    /// <summary>Read an invitation from the JSON the API sent.</summary>
    /// <param name="json">The invitation, as a JSON object.</param>
    public Invitation(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Id = JsonValues.String(data, "id");
        Email = JsonValues.String(data, "email");
        Role = JsonValues.String(data, "role");
        InvitedBy = JsonValues.NullableString(data, "invited_by");
        Expired = JsonValues.Bool(data, "expired");
        ExpiresAt = JsonValues.Date(data, "expires_at");
        CreatedAt = JsonValues.Date(data, "created_at");
    }

    /// <summary>The invitation's id, which is not the code in the email's link.</summary>
    public string Id { get; }

    /// <summary>Where it was sent.</summary>
    public string Email { get; }

    /// <summary>The role it gives once accepted, one of the <see cref="MemberRole"/> values.</summary>
    public string Role { get; }

    /// <summary>The name of the member who sent it.</summary>
    public string? InvitedBy { get; }

    /// <summary>Whether it can no longer be accepted.</summary>
    public bool Expired { get; }

    /// <summary>When it stops working.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>When it was sent.</summary>
    public DateTimeOffset? CreatedAt { get; }
}
