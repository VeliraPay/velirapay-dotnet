using VeliraPay.Tests.Support;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

public sealed class WebhookEndpointTests
{
    private static readonly JsonObject Endpoint = Json("""
        {"id": "12", "object": "webhook_endpoint", "url": "https://example.com/hooks", "description": null, "events": ["charge.paid"],
         "active": true, "verified": true, "deliveries_count": 0, "failed_deliveries_count": 0,
         "verified_at": "2026-10-20T10:00:00.000000Z", "created_at": "2026-10-20T09:00:00.000000Z"}
        """).AsObject();

    private static readonly JsonObject Delivery = Json("""
        {"id": "9d2e0b8a-0000-4000-8000-5f3a00000000", "object": "webhook_delivery", "event": "charge.paid", "event_id": null, "test": true,
         "status": "succeeded", "attempts": 1, "response_status": 200, "error": null, "charge": null, "invoice": null,
         "created_at": "2026-11-06T09:00:00.000000Z", "last_attempted_at": "2026-11-06T09:00:01.000000Z", "delivered_at": "2026-11-06T09:00:01.000000Z"}
        """).AsObject();

    [Fact]
    public async Task EndpointsAreListedWithWhetherTheyCanReceive()
    {
        var http = new MockHttp().Json(new JsonObject
        {
            ["data"] = new JsonArray(
                Endpoint.With(("description", "Orders"), ("deliveries_count", 2), ("failed_deliveries_count", 1)),
                Endpoint.With(("id", "13"), ("url", "https://example.com/new"), ("events", new JsonArray()), ("verified", false), ("verified_at", null))),
            ["meta"] = Json("""{"current_page":1,"last_page":1,"per_page":25,"total":2}"""),
        });

        var page = await Client(http).WebhookEndpoints.ListAsync();
        var (verified, unverified) = (page[0], page[1]);

        Assert.Equal("/v1/webhook-endpoints", http.LastRequest.Path);
        Assert.Equal("12", verified.Id);
        Assert.Equal("https://example.com/hooks", verified.Url);
        Assert.Equal("Orders", verified.Description);
        Assert.Equal([EventType.ChargePaid], verified.Events);
        Assert.True(verified.IsReceiving);
        Assert.Equal(2, verified.DeliveriesCount);
        Assert.Equal(1, verified.FailedDeliveriesCount);
        Assert.Equal(Utc("2026-10-20T10:00:00Z"), verified.VerifiedAt);
        Assert.Null(verified.Secret);

        Assert.False(unverified.IsReceiving);
        Assert.Null(unverified.VerifiedAt);
        Assert.Empty(unverified.Events);
    }

    [Fact]
    public async Task AnEndpointIsAddedWithItsSecretAndWhetherItAnswered()
    {
        var http = new MockHttp().Json(new JsonObject
        {
            ["data"] = Endpoint.With(("verified", false), ("secret", "whsec_abc")),
            ["meta"] = Json("""{"verification_error":"The endpoint answered the challenge with HTTP 404."}"""),
        }, 201);

        var added = await Client(http).WebhookEndpoints.CreateAsync(
            new WebhookEndpointCreateOptions { Url = "https://example.com/hooks", Events = [EventType.ChargePaid], Active = true },
            new RequestOptions { IdempotencyKey = "add-hook" });

        var request = http.LastRequest;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/webhook-endpoints", request.Path);
        Assert.Equal("add-hook", request.Header("Idempotency-Key"));
        Assert.Equal("""{"url":"https://example.com/hooks","events":["charge.paid"],"active":true}""", request.Body);
        Assert.Equal("whsec_abc", added.Secret);
        Assert.Equal("The endpoint answered the challenge with HTTP 404.", added.VerificationError);
    }

    [Fact]
    public async Task AnEndpointIsChangedVerifiedTestedRotatedAndDeleted()
    {
        var http = new MockHttp()
            .Json(new JsonObject { ["data"] = Endpoint.With(("active", false)), ["meta"] = Json("""{"verification_error":null}""") })
            .Json(Data(Endpoint))
            .Json(Data(Delivery), 201)
            .Json(new JsonObject { ["data"] = Endpoint.With(("secret", "whsec_new")), ["meta"] = Json("""{"previous_secret_expires_at":"2026-11-07T09:00:00.000000Z"}""") })
            .Empty();
        var client = Client(http);

        var updated = await client.WebhookEndpoints.UpdateAsync("12", new WebhookEndpointUpdateOptions { Active = false, Description = null });

        Assert.False(updated.Active);
        Assert.Null(updated.VerificationError);
        Assert.Equal(HttpMethod.Patch, http.LastRequest.Method);
        Assert.Equal("/v1/webhook-endpoints/12", http.LastRequest.Path);
        Assert.Equal("""{"active":false,"description":null}""", http.LastRequest.Body);

        Assert.True((await client.WebhookEndpoints.VerifyAsync("12")).Verified);
        Assert.Equal("/v1/webhook-endpoints/12/verify", http.LastRequest.Path);

        Assert.True((await client.WebhookEndpoints.TestAsync("12")).Test);
        Assert.Equal("/v1/webhook-endpoints/12/test", http.LastRequest.Path);

        var rotated = await client.WebhookEndpoints.RotateSecretAsync("12");

        Assert.Equal("whsec_new", rotated.Secret);
        Assert.Equal(Utc("2026-11-07T09:00:00Z"), rotated.PreviousSecretExpiresAt);
        Assert.Equal("/v1/webhook-endpoints/12/secret", http.LastRequest.Path);

        await client.WebhookEndpoints.DeleteAsync("12");

        Assert.Equal(HttpMethod.Delete, http.LastRequest.Method);
        Assert.Equal("/v1/webhook-endpoints/12", http.LastRequest.Path);
    }

    [Fact]
    public async Task DeliveriesAreListedAndRetried()
    {
        var http = new MockHttp()
            .Json(new JsonObject
            {
                ["data"] = new JsonArray(Delivery.With(("test", false), ("status", "failed"), ("response_status", 500), ("charge", "abc123"))),
                ["meta"] = Json("""{"current_page":1,"last_page":1,"per_page":25,"total":1}"""),
            })
            .Json(Data(Delivery.With(("status", "pending"))));
        var client = Client(http);

        var listed = Assert.Single(await client.WebhookEndpoints.DeliveriesAsync("12", new WebhookDeliveryListOptions { Status = "failed" }));

        Assert.Equal("/v1/webhook-endpoints/12/deliveries", http.LastRequest.Path);
        Assert.Equal("status=failed", http.LastRequest.Query);
        Assert.False(listed.Succeeded);
        Assert.Equal(500, listed.ResponseStatus);
        Assert.Equal("abc123", listed.Charge);
        Assert.Null(listed.Invoice);
        Assert.Null(listed.Payload);
        Assert.Empty(listed.AttemptLog);

        var retried = await client.WebhookEndpoints.RetryDeliveryAsync("12", listed.Id);

        Assert.Equal(HttpMethod.Post, http.LastRequest.Method);
        Assert.Equal($"/v1/webhook-endpoints/12/deliveries/{listed.Id}/retry", http.LastRequest.Path);
        Assert.Equal("pending", retried.Status);
    }

    [Fact]
    public async Task ADeliveryIsRetrievedWithEveryAttemptAndItsBody()
    {
        var http = new MockHttp().Json(Data(Delivery.With(
            ("attempts", 2),
            ("payload", Json("""{"id":"whd_1","event":"charge.paid","data":{"charge":{"id":"abc123"}}}""")),
            ("attempt_log", Json("""
                [
                    {"object": "webhook_delivery_attempt", "attempt": 2, "url": "https://example.com/hooks",
                     "request_headers": {"Content-Type": "application/json", "X-VeliraPay-Signature": "t=1762419601,v1=…"},
                     "response_status": 200, "response_headers": {"Content-Type": "text/plain"}, "response_body": "ok",
                     "error": null, "duration_ms": 84, "created_at": "2026-11-06T09:05:00.000000Z"},
                    {"object": "webhook_delivery_attempt", "attempt": 1, "url": "https://example.com/hooks", "request_headers": {"Content-Type": "application/json"},
                     "response_status": null, "response_headers": null, "response_body": null,
                     "error": "Timed out after 10 seconds.", "duration_ms": 10003, "created_at": "2026-11-06T09:00:00.000000Z"}
                ]
                """)))));

        var read = await Client(http).WebhookEndpoints.RetrieveDeliveryAsync("12", "9d2e0b8a-0000-4000-8000-5f3a00000000");

        Assert.Equal(HttpMethod.Get, http.LastRequest.Method);
        Assert.Equal("/v1/webhook-endpoints/12/deliveries/9d2e0b8a-0000-4000-8000-5f3a00000000", http.LastRequest.Path);
        Assert.NotNull(read.Payload);
        Assert.Equal("whd_1", read.Payload["id"].GetString());
        Assert.Equal("abc123", read.Payload["data"].GetProperty("charge").GetProperty("id").GetString());
        Assert.Equal(2, read.AttemptLog.Count);

        var (latest, first) = (read.AttemptLog[0], read.AttemptLog[1]);

        Assert.True(latest.Succeeded);
        Assert.Equal(2, latest.Attempt);
        Assert.Equal("https://example.com/hooks", latest.Url);
        Assert.Equal("t=1762419601,v1=…", latest.RequestHeaders?["X-VeliraPay-Signature"]);
        Assert.Equal("text/plain", latest.ResponseHeaders?["content-type"]);
        Assert.Equal("ok", latest.ResponseBody);
        Assert.Equal(84, latest.DurationMs);
        Assert.Equal(Utc("2026-11-06T09:05:00Z"), latest.CreatedAt);
        Assert.False(first.Succeeded);
        Assert.Null(first.ResponseStatus);
        Assert.Null(first.ResponseHeaders);
        Assert.Equal("Timed out after 10 seconds.", first.Error);
    }
}
