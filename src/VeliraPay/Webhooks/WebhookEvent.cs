using System.Text.Encodings.Web;
using System.Text.Json;

namespace VeliraPay;

/// <summary>A webhook delivery's payload.</summary>
public sealed class WebhookEvent : ApiResource
{
    /// <summary>Read a delivery from its decoded payload, without checking its signature.</summary>
    /// <param name="json">The payload, as a JSON object.</param>
    public WebhookEvent(JsonElement json)
        : base(json)
    {
        var data = RawJson;
        var objects = JsonValues.Object(data, "data");

        Id = JsonValues.String(data, "id");
        EventId = JsonValues.NullableString(data, "event_id");
        Type = JsonValues.String(data, "event");
        Mode = JsonValues.String(data, "mode");
        Test = JsonValues.Bool(data, "test");
        CreatedAt = JsonValues.Date(data, "created_at");
        Charge = JsonValues.NullableObject(objects, "charge", charge => new Charge(charge));
        Invoice = JsonValues.NullableObject(objects, "invoice", invoice => new Invoice(invoice));
        Transaction = JsonValues.NullableObject(objects, "transaction", transaction => new Transaction(transaction));
        Payout = JsonValues.NullableObject(objects, "payout", payout => new Payout(payout));
        Challenge = Type == Webhook.VerificationEvent && JsonValues.TryGet(objects, "challenge", out var challenge) && challenge.ValueKind == JsonValueKind.String
            ? challenge.GetString()
            : null;
    }

    /// <summary>The delivery's id, also sent as the X-VeliraPay-Delivery header; retries keep it, but every endpoint's copy of an event has its own.</summary>
    public string Id { get; }

    /// <summary>The event's id in the events API, the same on every endpoint's copy, so the one to skip duplicates by; null for test deliveries.</summary>
    public string? EventId { get; }

    /// <summary>One of the <see cref="EventType"/> values, such as "charge.paid", or <see cref="Webhook.VerificationEvent"/>.</summary>
    public string Type { get; }

    /// <summary>Either "live" or "test".</summary>
    public string Mode { get; }

    /// <summary>Whether this is a sample sent with the dashboard's "Send test event" button.</summary>
    public bool Test { get; }

    /// <summary>When the delivery was created.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>The charge, for charge.* and payout.* events.</summary>
    public Charge? Charge { get; }

    /// <summary>The invoice, for invoice.* events.</summary>
    public Invoice? Invoice { get; }

    /// <summary>The transfer the event is about, for charge.payment_detected and charge.late_payment.</summary>
    public Transaction? Transaction { get; }

    /// <summary>The payout the event is about, for payout.sent and payout.confirmed.</summary>
    public Payout? Payout { get; }

    /// <summary>The value a verification delivery must be answered with, or null for every other delivery.</summary>
    public string? Challenge { get; }

    /// <summary>Whether the event concerns real funds.</summary>
    public bool IsLiveMode => Mode == VeliraPay.Mode.Live;

    /// <summary>Whether this is the challenge VeliraPay sends when an endpoint is added or moved, to be answered with <see cref="ChallengeResponse"/>.</summary>
    public bool IsVerification => Type == Webhook.VerificationEvent;

    /// <summary>Whether the event is of any of the given types.</summary>
    /// <param name="types">The <see cref="EventType"/> values, or <see cref="Webhook.VerificationEvent"/>, to look for.</param>
    public bool Is(params string[] types) => Array.IndexOf(types, Type) >= 0;

    /// <summary>The JSON body that answers a verification delivery, <c>{"challenge":"…"}</c>, sent with HTTP 200; null for every other delivery.</summary>
    public string? ChallengeResponse()
    {
        if (Challenge is null)
        {
            return null;
        }

        var buffer = new System.Buffers.ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writer.WriteString("challenge", Challenge);
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>Read a delivery's raw body, without checking its signature.</summary>
    /// <param name="payload">The raw request body.</param>
    /// <exception cref="InvalidArgumentException">The payload is not a JSON object.</exception>
    public static WebhookEvent FromPayload(string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return FromPayload(System.Text.Encoding.UTF8.GetBytes(payload));
    }

    /// <summary>Read a delivery's raw body, without checking its signature.</summary>
    /// <param name="payload">The raw request body, as UTF-8.</param>
    /// <exception cref="InvalidArgumentException">The payload is not valid UTF-8 JSON, or not a JSON object.</exception>
    public static WebhookEvent FromPayload(ReadOnlySpan<byte> payload)
    {
        JsonElement root;

        try
        {
            using var document = JsonDocument.Parse(payload.ToArray());
            root = document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            throw new InvalidArgumentException($"The webhook payload is not valid JSON: {exception.Message}", exception);
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidArgumentException("The webhook payload is not a JSON object.");
        }

        return new WebhookEvent(root);
    }
}
