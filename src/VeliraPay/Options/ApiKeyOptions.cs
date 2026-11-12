namespace VeliraPay;

/// <summary>A new API key, in the mode of the key that makes it, which it can reach no further than.</summary>
public sealed class ApiKeyCreateOptions : BaseOptions
{
    /// <summary>What the key is for.</summary>
    public required string Name { get => Get<string>("name")!; set => Set("name", value); }

    /// <summary>The <see cref="ApiKeyPermission"/> values it has; leave out, or null, for full access.</summary>
    public IReadOnlyList<string>? Permissions { get => Get<IReadOnlyList<string>>("permissions"); set => Set("permissions", value); }

    /// <summary>Up to 20 IPv4 or IPv6 addresses or ranges it may be used from; leave out, or null, for anywhere.</summary>
    public IReadOnlyList<string>? AllowedIps { get => Get<IReadOnlyList<string>>("allowed_ips"); set => Set("allowed_ips", value); }
}

/// <summary>Changes to an API key: only the properties you set are changed. The key itself stays the same.</summary>
public sealed class ApiKeyUpdateOptions : BaseOptions
{
    /// <summary>What the key is for.</summary>
    public string? Name { get => Get<string>("name"); set => Set("name", value); }

    /// <summary>The <see cref="ApiKeyPermission"/> values it has; null for full access.</summary>
    public IReadOnlyList<string>? Permissions { get => Get<IReadOnlyList<string>>("permissions"); set => Set("permissions", value); }

    /// <summary>The IPv4 or IPv6 addresses or ranges it may be used from; null for anywhere.</summary>
    public IReadOnlyList<string>? AllowedIps { get => Get<IReadOnlyList<string>>("allowed_ips"); set => Set("allowed_ips", value); }
}
