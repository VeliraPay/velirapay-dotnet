using System.Text.Json;

namespace VeliraPay;

/// <summary>Where webhooks are delivered, and whether it can receive them. Its signing secret is only in the answers that add it or rotate it.</summary>
public sealed class WebhookEndpoint : ApiResource
{
    /// <summary>Read a webhook endpoint from the JSON the API sent.</summary>
    /// <param name="json">The endpoint, as a JSON object.</param>
    /// <param name="meta">What the answer said about it besides, such as why it did not answer its verification challenge.</param>
    public WebhookEndpoint(JsonElement json, JsonElement? meta = null)
        : base(json)
    {
        var data = RawJson;
        var about = meta?.ValueKind == JsonValueKind.Object ? meta.Value : JsonValues.EmptyObject;

        Id = JsonValues.String(data, "id");
        Url = JsonValues.String(data, "url");
        Description = JsonValues.NullableString(data, "description");
        Events = JsonValues.Strings(data, "events");
        Active = JsonValues.Bool(data, "active");
        Verified = JsonValues.Bool(data, "verified");
        DeliveriesCount = JsonValues.Int(data, "deliveries_count");
        FailedDeliveriesCount = JsonValues.Int(data, "failed_deliveries_count");
        VerifiedAt = JsonValues.Date(data, "verified_at");
        CreatedAt = JsonValues.Date(data, "created_at");
        Secret = JsonValues.NullableString(data, "secret");
        VerificationError = JsonValues.NullableString(about, "verification_error");
        PreviousSecretExpiresAt = JsonValues.Date(about, "previous_secret_expires_at");
    }

    /// <summary>The endpoint's id.</summary>
    public string Id { get; }

    /// <summary>Where deliveries go.</summary>
    public string Url { get; }

    /// <summary>What the merchant noted about it.</summary>
    public string? Description { get; }

    /// <summary>The <see cref="EventType"/> values it subscribes to.</summary>
    public IReadOnlyList<string> Events { get; }

    /// <summary>Whether it is switched on.</summary>
    public bool Active { get; }

    /// <summary>Whether it has answered the verification challenge; it receives nothing until it has.</summary>
    public bool Verified { get; }

    /// <summary>How many deliveries are kept for it, from the last 30 days.</summary>
    public int DeliveriesCount { get; }

    /// <summary>How many of those failed.</summary>
    public int FailedDeliveriesCount { get; }

    /// <summary>When it last answered the verification challenge.</summary>
    public DateTimeOffset? VerifiedAt { get; }

    /// <summary>When it was added.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>The signing secret, only in the answers that add the endpoint or rotate it: store it then, as it is never shown again.</summary>
    public string? Secret { get; }

    /// <summary>Why the endpoint did not answer the verification challenge, in the answers that sent it one.</summary>
    public string? VerificationError { get; }

    /// <summary>Until when the secret a rotation replaced still signs deliveries too, in the answer that rotates it.</summary>
    public DateTimeOffset? PreviousSecretExpiresAt { get; }

    /// <summary>Whether the endpoint receives deliveries right now.</summary>
    public bool IsReceiving => Active && Verified;
}

/// <summary>One event sent to a webhook endpoint, and how it went. What was sent and what came back are only in the answer of <see cref="WebhookEndpointService.RetrieveDeliveryAsync"/>.</summary>
public sealed class WebhookDelivery : ApiResource
{
    /// <summary>Read a delivery from the JSON the API sent.</summary>
    /// <param name="json">The delivery, as a JSON object.</param>
    public WebhookDelivery(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Id = JsonValues.String(data, "id");
        Event = JsonValues.String(data, "event");
        EventId = JsonValues.NullableString(data, "event_id");
        Test = JsonValues.Bool(data, "test");
        Status = JsonValues.String(data, "status");
        Attempts = JsonValues.Int(data, "attempts");
        ResponseStatus = JsonValues.NullableInt(data, "response_status");
        Error = JsonValues.NullableString(data, "error");
        Charge = JsonValues.Reference(data, "charge");
        Invoice = JsonValues.Reference(data, "invoice");
        CreatedAt = JsonValues.Date(data, "created_at");
        LastAttemptedAt = JsonValues.Date(data, "last_attempted_at");
        DeliveredAt = JsonValues.Date(data, "delivered_at");
        Payload = JsonValues.TryGet(data, "payload", out var payload) && payload.ValueKind is JsonValueKind.Object or JsonValueKind.Array
            ? JsonValues.Map(data, "payload")
            : null;
        AttemptLog = JsonValues.Objects(data, "attempt_log", attempt => new WebhookDeliveryAttempt(attempt));
    }

    /// <summary>The delivery's id, the X-VeliraPay-Delivery header it was sent with.</summary>
    public string Id { get; }

    /// <summary>The <see cref="EventType"/> value it carried.</summary>
    public string Event { get; }

    /// <summary>The id of the event it carried, or null for a test delivery.</summary>
    public string? EventId { get; }

    /// <summary>Whether it was a test event, which describes no real payment.</summary>
    public bool Test { get; }

    /// <summary>"pending", "succeeded" or "failed".</summary>
    public string Status { get; }

    /// <summary>How many times it was sent.</summary>
    public int Attempts { get; }

    /// <summary>The HTTP status the endpoint last answered with.</summary>
    public int? ResponseStatus { get; }

    /// <summary>Why the last attempt failed.</summary>
    public string? Error { get; }

    /// <summary>The id of the charge it was about.</summary>
    public string? Charge { get; }

    /// <summary>The id of the invoice it was about.</summary>
    public string? Invoice { get; }

    /// <summary>When it was queued.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>When it was last sent.</summary>
    public DateTimeOffset? LastAttemptedAt { get; }

    /// <summary>When the endpoint accepted it.</summary>
    public DateTimeOffset? DeliveredAt { get; }

    /// <summary>The JSON body it carried, from <see cref="WebhookEndpointService.RetrieveDeliveryAsync"/> alone and only for a key that may also read events; null otherwise.</summary>
    public IReadOnlyDictionary<string, JsonElement>? Payload { get; }

    /// <summary>Every attempt to send it, the latest first, from <see cref="WebhookEndpointService.RetrieveDeliveryAsync"/> alone.</summary>
    public IReadOnlyList<WebhookDeliveryAttempt> AttemptLog { get; }

    /// <summary>Whether the endpoint accepted the delivery.</summary>
    public bool Succeeded => Status == "succeeded";
}

/// <summary>One attempt to send a webhook delivery: the request it made and what the endpoint answered.</summary>
public sealed class WebhookDeliveryAttempt : ApiResource
{
    /// <summary>Read an attempt from the JSON the API sent.</summary>
    /// <param name="json">The attempt, as a JSON object.</param>
    public WebhookDeliveryAttempt(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Attempt = JsonValues.Int(data, "attempt");
        Url = JsonValues.NullableString(data, "url");
        RequestHeaders = Headers(data, "request_headers");
        ResponseStatus = JsonValues.NullableInt(data, "response_status");
        ResponseHeaders = Headers(data, "response_headers");
        ResponseBody = JsonValues.NullableString(data, "response_body");
        Error = JsonValues.NullableString(data, "error");
        DurationMs = JsonValues.NullableInt(data, "duration_ms");
        CreatedAt = JsonValues.Date(data, "created_at");
    }

    /// <summary>Which attempt it was, counted from 1.</summary>
    public int Attempt { get; }

    /// <summary>Where it was sent, or null when nothing was sent.</summary>
    public string? Url { get; }

    /// <summary>The headers sent, the signatures cut out but the timestamp they signed kept; null when nothing was sent.</summary>
    public IReadOnlyDictionary<string, string>? RequestHeaders { get; }

    /// <summary>The HTTP status the endpoint answered with, or null when nothing answered.</summary>
    public int? ResponseStatus { get; }

    /// <summary>The headers the endpoint answered with, a few dozen at most and never cookies.</summary>
    public IReadOnlyDictionary<string, string>? ResponseHeaders { get; }

    /// <summary>The first 2,000 characters of the endpoint's answer.</summary>
    public string? ResponseBody { get; }

    /// <summary>Why the attempt failed, such as a timeout or a refused connection.</summary>
    public string? Error { get; }

    /// <summary>How long the endpoint took, in milliseconds.</summary>
    public int? DurationMs { get; }

    /// <summary>When the attempt was made.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>Whether the endpoint answered the attempt with a 2xx status.</summary>
    public bool Succeeded => ResponseStatus is >= 200 and < 300;

    /// <summary>Read a map of header names to values, or null when none were kept.</summary>
    private static Dictionary<string, string>? Headers(JsonElement data, string key)
    {
        if (!JsonValues.TryGet(data, key, out var value) || value.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
        {
            return null;
        }

        return JsonValues.EnumerateMap(data, key)
            .Where(entry => entry.Value.ValueKind == JsonValueKind.String)
            .GroupBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value.GetString()!, StringComparer.OrdinalIgnoreCase);
    }
}
