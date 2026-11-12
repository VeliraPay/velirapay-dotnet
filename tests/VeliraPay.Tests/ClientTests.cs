using VeliraPay.Tests.Support;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

public sealed class ClientTests
{
    [Fact]
    public async Task RequestsAreAuthenticatedAndIdentifyTheLibrary()
    {
        var http = new MockHttp().Json(Fixture("account"));

        await Client(http, appInfo: "MyShop/2.1").Account.RetrieveAsync();

        var request = http.LastRequest;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api.velirapay.com/v1/account", request.Uri.ToString());
        Assert.Equal("Bearer vp_test_secret", request.Header("Authorization"));
        Assert.Equal("application/json", request.Header("Accept"));
        Assert.Equal($"VeliraPay-DotNet/{VeliraPayClient.Version} .NET/{Environment.Version} MyShop/2.1", request.Header("User-Agent"));
        Assert.Null(request.Header("Idempotency-Key"));
        Assert.Null(request.Header("Content-Type"));
        Assert.Null(request.Body);
    }

    [Fact]
    public void EveryGroupOfEndpointsIsThere()
    {
        var client = Client(new MockHttp());

        object[] services =
        [
            client.Account, client.Assets, client.Charges, client.PaymentLinks, client.Invoices, client.Coupons, client.Payouts, client.Events,
            client.WebhookEndpoints, client.Overview, client.PayoutAddresses, client.Activity, client.ApiKeys, client.Members, client.Invitations, client.Billing,
        ];

        Assert.Equal(16, services.Distinct().Count());
        Assert.All(services, Assert.NotNull);
    }

    [Fact]
    public void TheModeIsReadFromTheKeyPrefix()
    {
        var http = new MockHttp();

        Assert.Equal(Mode.Live, Client(http, "vp_live_secret").Mode);
        Assert.True(Client(http, "vp_live_secret").IsLiveMode);
        Assert.False(Client(http, "vp_live_secret").IsTestMode);
        Assert.Equal(Mode.Test, Client(http, "vp_test_secret").Mode);
        Assert.True(Client(http, "vp_test_secret").IsTestMode);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("")]
    [InlineData(null)]
    public void AMissingKeyIsRejected(string? apiKey)
    {
        var exception = Assert.Throws<InvalidArgumentException>(() => new VeliraPayClient(apiKey));

        Assert.Contains("API key is required", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("sk_live_secret")]
    [InlineData("vp_secret")]
    [InlineData("VP_LIVE_secret")]
    public void AKeyThatDoesNotStartWithAModeIsRejected(string apiKey)
    {
        var exception = Assert.Throws<InvalidArgumentException>(() => new VeliraPayClient(apiKey));

        Assert.Contains("starts with \"vp_live_\" or \"vp_test_\"", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("vp_test_secret\n")]
    [InlineData("vp_test_sec ret")]
    public void AKeyWithASpaceOrALineBreakIsRejectedWithoutRepeatingIt(string apiKey)
    {
        var exception = Assert.Throws<InvalidArgumentException>(() => new VeliraPayClient(apiKey));

        Assert.DoesNotContain("secret", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ImpossibleOptionsAreRejected()
    {
        var http = new HttpClient(new MockHttp());

        Assert.Throws<InvalidArgumentException>(() => new VeliraPayClient("vp_test_secret", http, new VeliraPayClientOptions { MaxRetries = -1 }));
        Assert.Throws<InvalidArgumentException>(() => new VeliraPayClient("vp_test_secret", http, new VeliraPayClientOptions { MaxRetryAfter = TimeSpan.FromSeconds(-1) }));
        Assert.Throws<InvalidArgumentException>(() => new VeliraPayClient("vp_test_secret", http, new VeliraPayClientOptions { Timeout = TimeSpan.Zero }));
        Assert.Throws<InvalidArgumentException>(() => new VeliraPayClient("vp_test_secret", http, new VeliraPayClientOptions { Timeout = TimeSpan.FromDays(30) }));
        Assert.Throws<InvalidArgumentException>(() => new VeliraPayClient("vp_test_secret", http, new VeliraPayClientOptions { BaseUrl = "api.velirapay.com" }));
        Assert.Throws<InvalidArgumentException>(() => new VeliraPayClient("vp_test_secret", http, new VeliraPayClientOptions { BaseUrl = "ftp://api.velirapay.com" }));
        Assert.Throws<InvalidArgumentException>(() => new VeliraPayClient("vp_test_secret", http, new VeliraPayClientOptions { AppInfo = "MyShop\r\nX-Injected: 1" }));
        Assert.Throws<ArgumentNullException>(() => new VeliraPayClient("vp_test_secret", (HttpClient)null!));

        _ = new VeliraPayClient("vp_test_secret", http, new VeliraPayClientOptions { Timeout = Timeout.InfiniteTimeSpan, MaxRetries = 0, MaxRetryAfter = TimeSpan.Zero });
    }

    [Fact]
    public async Task ARetryAfterLongerThanTheConfiguredLongestIsThrownStraightAway()
    {
        var http = new MockHttp().Json("""{"message":"Too Many Attempts."}""", 429, ("Retry-After", "6"));

        var exception = await Assert.ThrowsAsync<RateLimitException>(() => Client(http, maxRetries: 2, maxRetryAfter: 5).Charges.ListAsync());

        Assert.Equal(TimeSpan.FromSeconds(6), exception.RetryAfter);
        Assert.Single(http.Requests);
    }

    [Fact]
    public void TheSharedHttpClientIsUsedWhenNoneIsGiven()
    {
        Assert.Equal(Mode.Test, new VeliraPayClient("vp_test_secret").Mode);
        Assert.Equal(Mode.Live, new VeliraPayClient("vp_live_secret", new VeliraPayClientOptions { AppInfo = "MyShop/2.1" }).Mode);
    }

    [Fact]
    public async Task RequestsGoToACustomBaseUrl()
    {
        var http = new MockHttp().Json(Data(Fixture("charge")));

        await Client(http, baseUrl: "http://localhost:8000/").Charges.RetrieveAsync("k3v9x2m7q8wz");

        Assert.Equal("http://localhost:8000/v1/charges/k3v9x2m7q8wz", http.LastRequest.Uri.ToString());
    }

    [Fact]
    public async Task RawRequestsSendParamsAsTheQueryOrTheBody()
    {
        var http = new MockHttp().Json(ListResponse([])).Json(Data(Fixture("charge")), 201);
        var client = Client(http);

        await client.RequestAsync(HttpMethod.Get, "/v1/charges", new Dictionary<string, object?> { ["status"] = ChargeStatus.Paid, ["per_page"] = 5, ["payment_link"] = null });

        Assert.Equal("status=paid&per_page=5", http.LastRequest.Query);

        var response = await client.RequestAsync(HttpMethod.Post, "/v1/charges", new Dictionary<string, object?> { ["asset"] = "BTC", ["amount"] = 25.00m }, new RequestOptions { IdempotencyKey = "order-7" });

        Assert.Equal(201, response.Status);
        Assert.Equal("""{"asset":"BTC","amount":"25"}""", http.LastRequest.Body);
        Assert.Equal("application/json", http.LastRequest.Header("Content-Type"));
        Assert.Equal("order-7", http.LastRequest.Header("Idempotency-Key"));
        Assert.Same(response, client.LastResponse);
        Assert.Equal("k3v9x2m7q8wz", response.Json().GetProperty("data").GetProperty("id").GetString());
    }

    [Fact]
    public async Task ReplayedResponsesAreRecognised()
    {
        var http = new MockHttp().Json(Data(Fixture("charge")), 201, ("Idempotent-Replayed", "true"));
        var client = Client(http);

        Assert.Null(client.LastResponse);

        await client.Charges.CreateAsync(new ChargeCreateOptions { Asset = Asset.BTC, Amount = 150.00m, Currency = Currency.EUR }, new RequestOptions { IdempotencyKey = "order-1042" });

        Assert.True(client.LastResponse?.WasReplayed);
        AssertJson("""{"asset":"BTC","amount":"150","currency":"EUR"}""", http.LastRequest.Body);
    }

    [Fact]
    public async Task ABodyThatCannotBeEncodedIsRefusedBeforeAnythingIsSent()
    {
        var http = new MockHttp();
        var circular = new Dictionary<string, object?>();
        circular["self"] = circular;

        await Assert.ThrowsAsync<InvalidArgumentException>(() => Client(http).RequestAsync(HttpMethod.Post, "/v1/charges", circular));
        await Assert.ThrowsAsync<InvalidArgumentException>(() => Client(http).RequestAsync(HttpMethod.Post, "/v1/charges", new Dictionary<string, object?> { ["at"] = DateTime.UtcNow }));
        await Assert.ThrowsAsync<InvalidArgumentException>(() => Client(http).RequestAsync(HttpMethod.Post, "/v1/charges", new Dictionary<string, object?> { ["amount"] = double.NaN }));
        await Assert.ThrowsAsync<InvalidArgumentException>(() => Client(http).RequestAsync(HttpMethod.Get, "/v1/charges", new Dictionary<string, object?> { ["min"] = float.PositiveInfinity }));
        Assert.Empty(http.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("order-1042\r\nX-Injected: 1")]
    [InlineData("commande-n°7")]
    public async Task AnIdempotencyKeyThatCannotBeAHeaderIsRefusedBeforeAnythingIsSent(string idempotencyKey)
    {
        var http = new MockHttp();

        await Assert.ThrowsAsync<InvalidArgumentException>(() => Client(http).Charges.CancelAsync("k3v9x2m7q8wz", new RequestOptions { IdempotencyKey = idempotencyKey }));
        Assert.Empty(http.Requests);
    }

    [Fact]
    public async Task ACanceledRequestIsNeitherRetriedNorWrapped()
    {
        using var cancellation = new CancellationTokenSource();
        var http = new MockHttp().Answer(async (_, token) =>
        {
            await cancellation.CancelAsync();
            token.ThrowIfCancellationRequested();

            throw new InvalidOperationException("The request was not canceled.");
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Client(http, maxRetries: 2).Charges.ListAsync(cancellationToken: cancellation.Token));
        Assert.Single(http.Requests);
    }

    [Fact]
    public void TheVersionMatchesTheAssembly()
    {
        Assert.Equal(new Version(VeliraPayClient.Version + ".0"), typeof(VeliraPayClient).Assembly.GetName().Version);
    }

    [Fact]
    public void TheLibraryDependsOnNothingButTheBaseClassLibrary()
    {
        var references = typeof(VeliraPayClient).Assembly.GetReferencedAssemblies().Select(reference => reference.Name!).ToList();

        Assert.NotEmpty(references);
        Assert.All(references, name => Assert.True(name == "System" || name.StartsWith("System.", StringComparison.Ordinal), $"{name} is not part of .NET."));
    }
}
