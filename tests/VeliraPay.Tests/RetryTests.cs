using System.Net;
using System.Text.RegularExpressions;
using VeliraPay.Tests.Support;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

public sealed class RetryTests
{
    private static readonly Dictionary<string, object?> Body = new() { ["asset"] = "BTC" };

    private readonly List<TimeSpan> delays = [];

    [Fact]
    public async Task OutagesAndRateLimitsAreRetriedWithTheSameIdempotencyKey()
    {
        var http = new MockHttp()
            .Json("""{"message":"Service Unavailable"}""", 503)
            .Json("""{"message":"Too Many Attempts."}""", 429, ("Retry-After", "2"))
            .Json(Data(Fixture("charge")), 201);

        var response = await Transport(http, delays).RequestAsync(HttpMethod.Post, "/v1/charges", null, Body, null, default);

        Assert.Equal(201, response.Status);
        Assert.Equal(3, http.Requests.Count);

        var keys = http.Requests.Select(request => request.Header("Idempotency-Key")).Distinct().ToList();
        Assert.Single(keys);
        Assert.Matches(new Regex("^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$"), keys[0]);

        Assert.Equal(2, delays.Count);
        Assert.InRange(delays[0].TotalSeconds, 0.375, 0.625);
        Assert.Equal(TimeSpan.FromSeconds(2), delays[1]);
    }

    [Fact]
    public async Task AGivenIdempotencyKeyIsKept()
    {
        var http = new MockHttp().Json("""{"message":"Bad Gateway"}""", 502).Json(Data(Fixture("charge")), 201);

        await Transport(http, delays).RequestAsync(HttpMethod.Post, "/v1/charges", null, Body, "order-1042", default);

        Assert.Equal("order-1042", http.Requests[0].Header("Idempotency-Key"));
        Assert.Equal("order-1042", http.Requests[1].Header("Idempotency-Key"));
    }

    [Fact]
    public async Task AServerErrorIsRetriedForReadsOnly()
    {
        var http = new MockHttp()
            .Json("""{"message":"Server Error"}""", 500)
            .Json(Data(Fixture("charge")))
            .Json("""{"message":"Server Error"}""", 500);

        await Transport(http, delays).RequestAsync(HttpMethod.Get, "/v1/charges/k3v9x2m7q8wz", null, null, null, default);

        Assert.Equal(2, http.Requests.Count);

        await Assert.ThrowsAsync<ServerException>(() => Transport(http, delays).RequestAsync(HttpMethod.Post, "/v1/charges/k3v9x2m7q8wz/cancel", null, null, null, default));
        Assert.Equal(3, http.Requests.Count);
    }

    [Fact]
    public async Task ALongRetryAfterIsReportedRatherThanWaited()
    {
        var http = new MockHttp().Json("""{"message":"Too Many Attempts."}""", 429, ("Retry-After", "60"));

        var exception = await Assert.ThrowsAsync<RateLimitException>(() => Transport(http, delays).RequestAsync(HttpMethod.Get, "/v1/charges", null, null, null, default));

        Assert.Equal(TimeSpan.FromSeconds(60), exception.RetryAfter);
        Assert.Single(http.Requests);
        Assert.Empty(delays);
    }

    [Theory]
    [InlineData(409, typeof(ConflictException))]
    [InlineData(503, typeof(ServerException))]
    public async Task AnyRetryAfterLongerThanTheLongestIsThrownStraightAway(int status, Type exceptionType)
    {
        var http = new MockHttp().Json("""{"message":"Try again later."}""", status, ("Retry-After", "11"));

        var exception = await Assert.ThrowsAnyAsync<ApiException>(() => Transport(http, delays).RequestAsync(HttpMethod.Post, "/v1/charges", null, Body, null, default));

        Assert.IsType(exceptionType, exception);
        Assert.Equal(TimeSpan.FromSeconds(11), exception.RetryAfter);
        Assert.Single(http.Requests);
        Assert.Empty(delays);
    }

    [Fact]
    public async Task TheLongestRetryAfterThatIsWaitedOutCanBeRaised()
    {
        var http = new MockHttp().Json("""{"message":"Too Many Attempts."}""", 429, ("Retry-After", "60")).Json(ListResponse([]));

        await Transport(http, delays, maxRetryAfter: 60).RequestAsync(HttpMethod.Get, "/v1/charges", null, null, null, default);

        Assert.Equal(2, http.Requests.Count);
        Assert.Equal([TimeSpan.FromSeconds(60)], delays);
    }

    [Fact]
    public async Task AWriteStillBeingProcessedUnderTheSameKeyIsWaitedFor()
    {
        var http = new MockHttp()
            .Json("""{"message":"A request with this Idempotency-Key is still being processed."}""", 409, ("Retry-After", "1"))
            .Json(Data(Fixture("charge")), 201, ("Idempotent-Replayed", "true"));

        var response = await Transport(http, delays).RequestAsync(HttpMethod.Post, "/v1/charges", null, Body, "order-1042", default);

        Assert.Equal(201, response.Status);
        Assert.True(response.WasReplayed);
        Assert.Equal(2, http.Requests.Count);
        Assert.Equal("order-1042", http.Requests[1].Header("Idempotency-Key"));
        Assert.Equal([TimeSpan.FromSeconds(1)], delays);
    }

    [Fact]
    public async Task AConflictWithoutARetryAfterIsNotRetried()
    {
        var http = new MockHttp().Json("""{"message":"This Idempotency-Key was already used for a different request."}""", 409);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => Transport(http, delays).RequestAsync(HttpMethod.Post, "/v1/charges", null, Body, "order-1042", default));

        Assert.Null(exception.RetryAfter);
        Assert.False(exception.WasReplayed);
        Assert.Single(http.Requests);
        Assert.Empty(delays);
    }

    [Theory]
    [InlineData("api key")]
    [InlineData("webhook endpoint")]
    [InlineData("secret rotation")]
    public async Task AWriteWhoseLostAnswerCarriedASecretEndsInAReplayedConflictWhenRetried(string write)
    {
        var http = new MockHttp()
            .Throw(new HttpRequestException("An error occurred while sending the request.", new IOException("The response ended prematurely.")))
            .Json(
                """{"message":"A request with this Idempotency-Key already succeeded. Its answer carried a secret, which is shown once and not kept, so it cannot be replayed."}""",
                409,
                ("Idempotent-Replayed", "true"));
        var client = Client(http, maxRetries: 1, delays: delays);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => write switch
        {
            "api key" => client.ApiKeys.CreateAsync(new ApiKeyCreateOptions { Name = "Shop server" }),
            "webhook endpoint" => client.WebhookEndpoints.CreateAsync(new WebhookEndpointCreateOptions { Url = "https://example.com/hooks", Events = [EventType.ChargePaid] }),
            _ => client.WebhookEndpoints.RotateSecretAsync("12"),
        });

        Assert.True(exception.WasReplayed);
        Assert.Null(exception.RetryAfter);
        Assert.StartsWith("A request with this Idempotency-Key already succeeded.", exception.Message, StringComparison.Ordinal);
        Assert.Equal(2, http.Requests.Count);
        Assert.NotNull(http.Requests[0].Header("Idempotency-Key"));
        Assert.Equal(http.Requests[0].Header("Idempotency-Key"), http.Requests[1].Header("Idempotency-Key"));
        Assert.Equal(http.Requests[0].Body, http.Requests[1].Body);
        Assert.Single(delays);
    }

    [Fact]
    public async Task NetworkErrorsAreRetriedUntilTheLimit()
    {
        var failure = new HttpRequestException("Connection refused (api.velirapay.com:443)");
        var http = new MockHttp().Throw(failure).Throw(failure).Throw(failure);

        var exception = await Assert.ThrowsAsync<ConnectionException>(() => Transport(http, delays, maxRetries: 2).RequestAsync(HttpMethod.Get, "/v1/charges", null, null, null, default));

        Assert.Equal("Could not reach the VeliraPay API: Connection refused (api.velirapay.com:443)", exception.Message);
        Assert.Same(failure, exception.InnerException);
        Assert.Equal(3, http.Requests.Count);
        Assert.Equal(2, delays.Count);
        Assert.InRange(delays[1].TotalSeconds, 0.75, 1.25);
    }

    [Fact]
    public async Task ANetworkErrorIsRetriedAndTheRetryAnswered()
    {
        var http = new MockHttp().Throw(new HttpRequestException("An error occurred while sending the request.", new IOException("The response ended prematurely."))).Json(ListResponse([]));

        var response = await Transport(http, delays).RequestAsync(HttpMethod.Get, "/v1/charges", null, null, null, default);

        Assert.Equal(200, response.Status);
        Assert.Equal(2, http.Requests.Count);
    }

    [Fact]
    public async Task AnAnswerCutOffWhileItsBodyIsReadIsRetried()
    {
        var http = new MockHttp()
            .Answer((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new CutOffContent() }))
            .Json(Data(Fixture("charge")));

        var response = await Transport(http, delays).RequestAsync(HttpMethod.Get, "/v1/charges/k3v9x2m7q8wz", null, null, null, default);

        Assert.Equal(200, response.Status);
        Assert.Equal(2, http.Requests.Count);
    }

    [Fact]
    public async Task AGenericFailureIsExplainedByWhatCausedIt()
    {
        var http = new MockHttp().Throw(new HttpRequestException("An error occurred while sending the request.", new IOException("The response ended prematurely.")));

        var exception = await Assert.ThrowsAsync<ConnectionException>(() => Transport(http, delays, maxRetries: 0).RequestAsync(HttpMethod.Get, "/v1/charges", null, null, null, default));

        Assert.Equal("Could not reach the VeliraPay API: The response ended prematurely.", exception.Message);
    }

    [Fact]
    public async Task ClientErrorsAreNotRetried()
    {
        var http = new MockHttp().Json("""{"message":"The given data was invalid.","errors":[]}""", 422);

        await Assert.ThrowsAsync<ValidationException>(() => Transport(http, delays).RequestAsync(HttpMethod.Post, "/v1/charges", null, Body, null, default));
        Assert.Single(http.Requests);
    }

    [Fact]
    public async Task NoIdempotencyKeyIsAddedWhenRetriesAreOff()
    {
        var http = new MockHttp().Json(Data(Fixture("charge")), 201);

        await Transport(http, delays, maxRetries: 0).RequestAsync(HttpMethod.Post, "/v1/charges", null, Body, null, default);

        Assert.Null(http.LastRequest.Header("Idempotency-Key"));
    }

    [Fact]
    public async Task NoIdempotencyKeyIsAddedToARead()
    {
        var http = new MockHttp().Json(ListResponse([]));

        await Transport(http, delays).RequestAsync(HttpMethod.Get, "/v1/charges", null, null, null, default);

        Assert.Null(http.LastRequest.Header("Idempotency-Key"));
    }

    [Fact]
    public async Task AConnectionErrorIsNotRetriedWhenRetriesAreOff()
    {
        var http = new MockHttp().Throw(new HttpRequestException("Connection refused"));

        await Assert.ThrowsAsync<ConnectionException>(() => Transport(http, delays, maxRetries: 0).RequestAsync(HttpMethod.Get, "/v1/charges", null, null, null, default));
        Assert.Single(http.Requests);
    }

    [Fact]
    public async Task TheClientRetriesWithItsOwnSettings()
    {
        var http = new MockHttp().Json("""{"message":"Service Unavailable"}""", 503).Json(Data(Fixture("charge")), 201);

        var charge = await Client(http, maxRetries: 1, delays: delays).Charges.CreateAsync(new ChargeCreateOptions { Asset = Asset.BTC, Amount = 150m, Currency = Currency.EUR });

        Assert.Equal("k3v9x2m7q8wz", charge.Id);
        Assert.Equal(2, http.Requests.Count);
        Assert.Single(delays);
        Assert.NotNull(http.Requests[0].Header("Idempotency-Key"));
        Assert.Equal(http.Requests[0].Header("Idempotency-Key"), http.Requests[1].Header("Idempotency-Key"));
    }

    /// <summary>An answer whose body breaks off while it is read.</summary>
    private sealed class CutOffContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context) => throw new IOException("The response ended prematurely.");

        protected override bool TryComputeLength(out long length)
        {
            length = 0;

            return false;
        }
    }
}
