namespace VeliraPay.Tests.Support;

/// <summary>Clients, fixtures and answers for the tests.</summary>
internal static class TestSupport
{
    /// <summary>A client that sends its requests to the mocked handler, without retries unless asked, recording its waits.</summary>
    public static VeliraPayClient Client(MockHttp http, string apiKey = "vp_test_secret", int maxRetries = 0, int maxRetryAfter = 10, string? appInfo = null, string? baseUrl = null, List<TimeSpan>? delays = null)
        => new(apiKey, new HttpClient(http), new VeliraPayClientOptions
        {
            MaxRetries = maxRetries,
            MaxRetryAfter = TimeSpan.FromSeconds(maxRetryAfter),
            AppInfo = appInfo,
            BaseUrl = baseUrl ?? VeliraPayClient.DefaultBaseUrl,
        }, Recorder(delays ?? []));

    /// <summary>A transport that records its waits instead of sleeping.</summary>
    public static HttpTransport Transport(MockHttp http, List<TimeSpan> delays, int maxRetries = 2, int maxRetryAfter = 10)
        => new(new HttpClient(http), "vp_test_secret", "https://api.velirapay.com", maxRetries, TimeSpan.FromSeconds(maxRetryAfter), TimeSpan.FromSeconds(30), "VeliraPay-DotNet/test", Recorder(delays));

    /// <summary>A JSON fixture, exactly as it is on disk.</summary>
    public static string Payload(string name) => File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", $"{name}.json"));

    /// <summary>A JSON fixture, decoded.</summary>
    public static JsonObject Fixture(string name) => JsonNode.Parse(Payload(name))!.AsObject();

    /// <summary>An object wrapped as the API answers with it.</summary>
    public static JsonObject Data(JsonNode data) => new() { ["data"] = data.DeepClone() };

    /// <summary>Objects wrapped in a list answer.</summary>
    public static JsonObject ListResponse(IEnumerable<JsonNode> data, int currentPage = 1, int lastPage = 1, int perPage = 25, int? total = null)
    {
        var items = data.Select(item => item.DeepClone()).ToList();

        return new JsonObject
        {
            ["data"] = new JsonArray([.. items]),
            ["links"] = JsonNode.Parse("""{"first":null,"last":null,"prev":null,"next":null}"""),
            ["meta"] = new JsonObject
            {
                ["current_page"] = currentPage,
                ["from"] = items.Count == 0 ? null : ((currentPage - 1) * perPage) + 1,
                ["last_page"] = lastPage,
                ["path"] = "https://api.velirapay.com/v1/charges",
                ["per_page"] = perPage,
                ["to"] = items.Count == 0 ? null : ((currentPage - 1) * perPage) + items.Count,
                ["total"] = total ?? items.Count,
            },
        };
    }

    /// <summary>A copy of an object with some fields changed.</summary>
    public static JsonObject With(this JsonObject source, params (string Key, JsonNode? Value)[] changes)
    {
        var copy = source.DeepClone().AsObject();

        foreach (var (key, value) in changes)
        {
            copy[key] = value?.DeepClone();
        }

        return copy;
    }

    /// <summary>A copy of an object without some fields.</summary>
    public static JsonObject Without(this JsonObject source, params string[] keys)
    {
        var copy = source.DeepClone().AsObject();

        foreach (var key in keys)
        {
            copy.Remove(key);
        }

        return copy;
    }

    /// <summary>Parse JSON written out in a test.</summary>
    public static JsonNode Json(string json) => JsonNode.Parse(json)!;

    /// <summary>A JSON value to read a resource from.</summary>
    public static JsonElement Element(JsonNode json) => JsonDocument.Parse(json.ToJsonString()).RootElement.Clone();

    /// <summary>Assert that two JSON texts say the same, whatever the order of their keys.</summary>
    public static void AssertJson(string expected, string? actual)
    {
        Assert.NotNull(actual);

        if (!JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)))
        {
            Assert.Fail($"Expected JSON {expected}\nbut got {actual}");
        }
    }

    /// <summary>Assert that a JSON body says the same as a JSON node.</summary>
    public static void AssertJson(JsonNode expected, string? actual) => AssertJson(expected.ToJsonString(), actual);

    /// <summary>A timestamp in UTC.</summary>
    public static DateTimeOffset Utc(string iso) => DateTimeOffset.Parse(iso, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal);

    /// <summary>A delay that records how long it was asked to wait, and does not.</summary>
    private static Func<TimeSpan, CancellationToken, Task> Recorder(List<TimeSpan> delays) => (delay, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        delays.Add(delay);

        return Task.CompletedTask;
    };
}
