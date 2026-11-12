namespace VeliraPay;

/// <summary>A new webhook endpoint, which receives nothing until it answers the verification challenge sent to it.</summary>
public sealed class WebhookEndpointCreateOptions : BaseOptions
{
    /// <summary>A public https:// URL.</summary>
    public required string Url { get => Get<string>("url")!; set => Set("url", value); }

    /// <summary>The <see cref="EventType"/> values it receives.</summary>
    public required IReadOnlyList<string> Events { get => Get<IReadOnlyList<string>>("events")!; set => Set("events", value); }

    /// <summary>A note about what it is for.</summary>
    public string? Description { get => Get<string>("description"); set => Set("description", value); }

    /// <summary>Whether it is switched on.</summary>
    public bool? Active { get => Get<bool?>("active"); set => Set("active", value); }
}

/// <summary>Changes to a webhook endpoint: only the properties you set are changed. A new URL is verified again.</summary>
public sealed class WebhookEndpointUpdateOptions : BaseOptions
{
    /// <summary>A public https:// URL.</summary>
    public string? Url { get => Get<string>("url"); set => Set("url", value); }

    /// <summary>The <see cref="EventType"/> values it receives.</summary>
    public IReadOnlyList<string>? Events { get => Get<IReadOnlyList<string>>("events"); set => Set("events", value); }

    /// <summary>A note about what it is for; null clears it.</summary>
    public string? Description { get => Get<string>("description"); set => Set("description", value); }

    /// <summary>Whether it is switched on.</summary>
    public bool? Active { get => Get<bool?>("active"); set => Set("active", value); }
}

/// <summary>Which of a webhook endpoint's deliveries to list.</summary>
public sealed class WebhookDeliveryListOptions : ListOptions
{
    /// <summary>Only deliveries in this state: "pending", "succeeded" or "failed".</summary>
    public string? Status { get => Get<string>("status"); set => Set("status", value); }

    /// <summary>Only deliveries of this <see cref="EventType"/>.</summary>
    public string? Event { get => Get<string>("event"); set => Set("event", value); }
}
