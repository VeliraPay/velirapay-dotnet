using System.Globalization;
using VeliraPay.Tests.Support;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

public sealed class ErrorTests
{
    [Theory]
    [InlineData(400, typeof(InvalidRequestException))]
    [InlineData(401, typeof(AuthenticationException))]
    [InlineData(403, typeof(PermissionException))]
    [InlineData(404, typeof(NotFoundException))]
    [InlineData(409, typeof(ConflictException))]
    [InlineData(422, typeof(ValidationException))]
    [InlineData(429, typeof(RateLimitException))]
    [InlineData(500, typeof(ServerException))]
    [InlineData(503, typeof(ServerException))]
    public async Task EachErrorStatusHasItsOwnException(int status, Type exceptionType)
    {
        var http = new MockHttp().Json("""{"message":"Something went wrong."}""", status);

        var exception = await Assert.ThrowsAnyAsync<VeliraPayException>(() => Client(http).Charges.RetrieveAsync("k3v9x2m7q8wz"));

        Assert.IsType(exceptionType, exception);
        var apiException = Assert.IsAssignableFrom<ApiException>(exception);
        Assert.Equal(status, apiException.Status);
        Assert.Equal("Something went wrong.", apiException.Message);
        Assert.Equal("Something went wrong.", apiException.Body.GetProperty("message").GetString());
        Assert.Equal(status, apiException.Response.Status);
    }

    [Fact]
    public async Task AnObjectOfAnotherAccountOrModeIsNotFound()
    {
        var http = new MockHttp().Json("""{"message":"No such object."}""", 404);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => Client(http).Charges.RetrieveAsync("k3v9x2m7q8wz"));

        Assert.Equal("No such object.", exception.Message);
    }

    [Fact]
    public async Task AnErrorWithoutAJsonMessageStillExplainsItself()
    {
        var http = new MockHttp().Text("<html>Bad Gateway</html>", 502, "text/html");

        var exception = await Assert.ThrowsAsync<ServerException>(() => Client(http).Charges.RetrieveAsync("k3v9x2m7q8wz"));

        Assert.Equal("The VeliraPay API answered with HTTP 502.", exception.Message);
        Assert.Equal(JsonValueKind.Object, exception.Body.ValueKind);
        Assert.Empty(exception.Body.EnumerateObject());
    }

    [Fact]
    public async Task ARedirectIsNotFollowedButReported()
    {
        var http = new MockHttp().Text(string.Empty, 301, "text/html", ("Location", "https://velirapay.com/"));

        var exception = await Assert.ThrowsAsync<InvalidRequestException>(() => Client(http).Charges.RetrieveAsync("k3v9x2m7q8wz"));

        Assert.Equal("The VeliraPay API answered with HTTP 301.", exception.Message);
        Assert.Equal("https://velirapay.com/", exception.Header("Location"));
    }

    [Fact]
    public async Task ValidationErrorsAreKeyedByField()
    {
        var http = new MockHttp().Json("""
            {
                "message": "The asset field is required. (and 1 more error)",
                "errors": {
                    "asset": ["The asset field is required."],
                    "items.0.quantity": ["The items.0.quantity field must be at least 0.001."]
                }
            }
            """, 422);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => Client(http).Charges.CreateAsync(new ChargeCreateOptions { Asset = string.Empty }));

        Assert.Equal(2, exception.Errors.Count);
        Assert.Equal(["The asset field is required."], exception.Errors["asset"]);
        Assert.Equal(["The items.0.quantity field must be at least 0.001."], exception.Errors["items.0.quantity"]);
        Assert.Equal("The asset field is required.", exception.FirstError("asset"));
        Assert.Null(exception.FirstError("amount"));
    }

    [Fact]
    public async Task ValidationErrorsGivenAsAnEmptyListOrASingleMessageAreRead()
    {
        var http = new MockHttp()
            .Json("""{"message":"The given data was invalid.","errors":[]}""", 422)
            .Json("""{"message":"The amount is too low.","errors":{"amount":"The amount is too low.","asset":[1, "Pick a coin."]}}""", 422);

        Assert.Empty((await Assert.ThrowsAsync<ValidationException>(() => Client(http).Charges.ListAsync())).Errors);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => Client(http).Charges.ListAsync());

        Assert.Equal("The amount is too low.", exception.FirstError("amount"));
        Assert.Equal(["Pick a coin."], exception.Errors["asset"]);
    }

    [Fact]
    public async Task ARateLimitSaysWhenToTryAgain()
    {
        var http = new MockHttp().Json("""{"message":"Too Many Attempts."}""", 429, ("Retry-After", "42"));

        var exception = await Assert.ThrowsAsync<RateLimitException>(() => Client(http).Charges.ListAsync());

        Assert.Equal(TimeSpan.FromSeconds(42), exception.RetryAfter);
        Assert.Equal("42", exception.Header("Retry-After"));
        Assert.Equal("42", exception.Header("retry-after"));
    }

    [Fact]
    public async Task ARetryAfterGivenAsADateIsReadInSeconds()
    {
        var at = DateTimeOffset.UtcNow.AddSeconds(30).ToString("r", CultureInfo.InvariantCulture);
        var http = new MockHttp().Json("""{"message":"Service Unavailable"}""", 503, ("Retry-After", at));

        var exception = await Assert.ThrowsAsync<ServerException>(() => Client(http).Charges.ListAsync());

        Assert.NotNull(exception.RetryAfter);
        Assert.InRange(exception.RetryAfter.Value.TotalSeconds, 29, 31);
    }

    [Fact]
    public void ARetryAfterThatIsNeitherSecondsNorADateIsIgnored()
    {
        var headers = new Dictionary<string, IReadOnlyList<string>> { ["Retry-After"] = ["soon"] };

        Assert.Null(new ApiResponse(503, headers, string.Empty).RetryAfter);
        Assert.Null(new ApiResponse(503, new Dictionary<string, IReadOnlyList<string>>(), string.Empty).RetryAfter);
        Assert.Equal(TimeSpan.Zero, new ApiResponse(503, new Dictionary<string, IReadOnlyList<string>> { ["Retry-After"] = ["Wed, 21 Oct 2015 07:28:00 GMT"] }, string.Empty).RetryAfter);
    }

    [Fact]
    public async Task ASuccessfulResponseThatIsNotJsonIsReported()
    {
        var http = new MockHttp().Text("<html>Maintenance</html>", 200, "text/html");

        var exception = await Assert.ThrowsAsync<UnexpectedResponseException>(() => Client(http).Charges.RetrieveAsync("k3v9x2m7q8wz"));

        Assert.Equal(200, exception.Status);
    }

    [Fact]
    public async Task ASuccessfulResponseWithoutTheObjectAskedForIsReported()
    {
        var http = new MockHttp().Json("""{"data":[]}""").Json("""{"data":{}}""");

        var exception = await Assert.ThrowsAsync<UnexpectedResponseException>(() => Client(http).Charges.RetrieveAsync("k3v9x2m7q8wz"));

        Assert.Contains("without the object", exception.Message, StringComparison.Ordinal);
        await Assert.ThrowsAsync<UnexpectedResponseException>(() => Client(http).Charges.ListAsync());
    }

    [Fact]
    public async Task AnUnreachableApiIsReported()
    {
        var failure = new HttpRequestException("No such host is known. (api.velirapay.com:443)");
        var http = new MockHttp().Throw(failure);

        var exception = await Assert.ThrowsAsync<ConnectionException>(() => Client(http).Charges.ListAsync());

        Assert.IsAssignableFrom<VeliraPayException>(exception);
        Assert.Equal("Could not reach the VeliraPay API: No such host is known. (api.velirapay.com:443)", exception.Message);
        Assert.Same(failure, exception.InnerException);
    }

    [Fact]
    public async Task ARequestThatTakesTooLongIsReported()
    {
        var http = new MockHttp().Answer(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);

            throw new InvalidOperationException("The request was not timed out.");
        });
        var client = new VeliraPayClient("vp_test_secret", new HttpClient(http), new VeliraPayClientOptions { MaxRetries = 0, Timeout = TimeSpan.FromMilliseconds(50) });

        var exception = await Assert.ThrowsAsync<ConnectionException>(() => client.Charges.ListAsync());

        Assert.Equal("Could not reach the VeliraPay API: no answer within 0.05 seconds.", exception.Message);
    }

    [Fact]
    public async Task AnHttpClientsOwnTimeoutIsReportedToo()
    {
        var http = new MockHttp().Answer(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);

            throw new InvalidOperationException("The request was not timed out.");
        });
        var client = new VeliraPayClient("vp_test_secret", new HttpClient(http) { Timeout = TimeSpan.FromMilliseconds(50) }, new VeliraPayClientOptions { MaxRetries = 0 });

        var exception = await Assert.ThrowsAsync<ConnectionException>(() => client.Charges.ListAsync());

        Assert.StartsWith("Could not reach the VeliraPay API: ", exception.Message, StringComparison.Ordinal);
        Assert.Contains("HttpClient.Timeout", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnErrorTheHandlerThrowsForAnotherReasonIsNotTakenForANetworkError()
    {
        var http = new MockHttp();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Client(http, maxRetries: 2).Charges.ListAsync());

        Assert.Contains("No response was queued", exception.Message, StringComparison.Ordinal);
        Assert.Single(http.Requests);
    }
}
