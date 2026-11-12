using System.Text.Json;

namespace VeliraPay;

/// <summary>An API key of the account. The key itself is only in the answer that creates it.</summary>
public sealed class ApiKey : ApiResource
{
    /// <summary>Read an API key from the JSON the API sent.</summary>
    /// <param name="json">The key, as a JSON object.</param>
    public ApiKey(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Id = JsonValues.String(data, "id");
        Name = JsonValues.String(data, "name");
        Prefix = JsonValues.String(data, "prefix");
        Key = JsonValues.NullableString(data, "key");
        Mode = JsonValues.String(data, "mode");
        Permissions = JsonValues.NullableStrings(data, "permissions");
        AllowedIps = JsonValues.NullableStrings(data, "allowed_ips");
        CreatedBy = JsonValues.NullableString(data, "created_by");
        LastUsedAt = JsonValues.Date(data, "last_used_at");
        CreatedAt = JsonValues.Date(data, "created_at");
    }

    /// <summary>The key's id, which is not the key itself.</summary>
    public string Id { get; }

    /// <summary>What the key is for.</summary>
    public string Name { get; }

    /// <summary>The start of the key, such as "vp_live_a1b2", to tell keys apart.</summary>
    public string Prefix { get; }

    /// <summary>The key itself, only in the answer that created it: store it then, as it is never shown again.</summary>
    public string? Key { get; }

    /// <summary>"live" or "test".</summary>
    public string Mode { get; }

    /// <summary>The <see cref="ApiKeyPermission"/> values the key has, or null for full access.</summary>
    public IReadOnlyList<string>? Permissions { get; }

    /// <summary>The addresses and ranges it may be used from, or null for anywhere.</summary>
    public IReadOnlyList<string>? AllowedIps { get; }

    /// <summary>The name of the member who made it.</summary>
    public string? CreatedBy { get; }

    /// <summary>When it last made a request.</summary>
    public DateTimeOffset? LastUsedAt { get; }

    /// <summary>When it was made.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>Whether the key has every permission that full access gives.</summary>
    public bool HasFullAccess => Permissions is null;
}
