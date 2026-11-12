namespace VeliraPay;

/// <summary>Which entries of the activity log to list.</summary>
public sealed class ActivityListOptions : ListOptions
{
    /// <summary>Only entries of this action, such as "api_key.created", or "sensitive" for the sensitive ones.</summary>
    public string? Action { get => Get<string>("action"); set => Set("action", value); }

    /// <summary>Only what this person did.</summary>
    public string? Person { get => Get<string>("person"); set => Set("person", value); }

    /// <summary>A subject, or the start of an IP address.</summary>
    public string? Q { get => Get<string>("q"); set => Set("q", value); }

    /// <summary>Only entries from this day on.</summary>
    public DateOnly? From { get => Get<DateOnly?>("from"); set => Set("from", value); }

    /// <summary>Only entries up to and including this day.</summary>
    public DateOnly? To { get => Get<DateOnly?>("to"); set => Set("to", value); }

    /// <summary>The key's own mode, to leave out what belongs to none, such as sign-ins.</summary>
    public string? Network { get => Get<string>("network"); set => Set("network", value); }
}
