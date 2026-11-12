namespace VeliraPay;

/// <summary>Which events to list.</summary>
public sealed class EventListOptions : ListOptions
{
    /// <summary>Only events of this type, one of the <see cref="EventType"/> values.</summary>
    public string? Type { get => Get<string>("type"); set => Set("type", value); }

    /// <summary>Only the events of this charge, by its id.</summary>
    public string? Charge { get => Get<string>("charge"); set => Set("charge", value); }

    /// <summary>Only the events of this invoice, by its id.</summary>
    public string? Invoice { get => Get<string>("invoice"); set => Set("invoice", value); }
}
