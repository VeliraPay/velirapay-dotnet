using System.Text.Json;

namespace VeliraPay;

/// <summary>One thing done in the account, from its activity log.</summary>
public sealed class Activity : ApiResource
{
    /// <summary>Read an entry of the activity log from the JSON the API sent.</summary>
    /// <param name="json">The entry, as a JSON object.</param>
    public Activity(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Id = JsonValues.String(data, "id");
        Action = JsonValues.String(data, "action");
        Description = JsonValues.String(data, "description");
        Actor = JsonValues.NullableString(data, "actor");
        Subject = JsonValues.NullableString(data, "subject");
        Properties = JsonValues.Map(data, "properties");
        IpAddress = JsonValues.NullableString(data, "ip_address");
        Mode = JsonValues.NullableString(data, "mode");
        CreatedAt = JsonValues.Date(data, "created_at");
    }

    /// <summary>The entry's id.</summary>
    public string Id { get; }

    /// <summary>What was done, such as "api_key.created".</summary>
    public string Action { get; }

    /// <summary>What was done, in words.</summary>
    public string Description { get; }

    /// <summary>Who did it: a member's name or an API key's.</summary>
    public string? Actor { get; }

    /// <summary>What it was done to.</summary>
    public string? Subject { get; }

    /// <summary>The details kept with it.</summary>
    public IReadOnlyDictionary<string, JsonElement> Properties { get; }

    /// <summary>The address it came from.</summary>
    public string? IpAddress { get; }

    /// <summary>"live" or "test", or null for what belongs to neither, such as a sign-in.</summary>
    public string? Mode { get; }

    /// <summary>When it was done.</summary>
    public DateTimeOffset? CreatedAt { get; }
}
