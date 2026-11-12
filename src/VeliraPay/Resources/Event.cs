using System.Text.Json;

namespace VeliraPay;

/// <summary>Something that happened to a charge or an invoice, as the events API lists it.</summary>
public sealed class Event : ApiResource
{
    /// <summary>Read an event from the JSON the API sent.</summary>
    /// <param name="json">The event, as a JSON object.</param>
    public Event(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Id = JsonValues.String(data, "id");
        Type = JsonValues.String(data, "type");
        Details = JsonValues.Map(data, "details");
        CreatedAt = JsonValues.Date(data, "created_at");
        Charge = JsonValues.NullableObject(data, "charge", charge => new Charge(charge));
        Invoice = JsonValues.NullableObject(data, "invoice", invoice => new Invoice(invoice));
    }

    /// <summary>The event's id, as a webhook's event_id refers to it.</summary>
    public string Id { get; }

    /// <summary>One of the <see cref="EventType"/> values, such as "charge.paid".</summary>
    public string Type { get; }

    /// <summary>What the event recorded, such as the amount and transaction of a detected payment.</summary>
    public IReadOnlyDictionary<string, JsonElement> Details { get; }

    /// <summary>When it happened.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>The charge the event is about, as it is now, without its timeline.</summary>
    public Charge? Charge { get; }

    /// <summary>The invoice the event is about.</summary>
    public Invoice? Invoice { get; }

    /// <summary>Whether the event is of any of the given types.</summary>
    /// <param name="types">The <see cref="EventType"/> values to look for.</param>
    public bool Is(params string[] types) => Array.IndexOf(types, Type) >= 0;
}
