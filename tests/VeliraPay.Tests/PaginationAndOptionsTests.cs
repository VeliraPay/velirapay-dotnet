using VeliraPay.Tests.Support;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

public sealed class PaginationAndOptionsTests
{
    [Fact]
    public async Task TheIteratorWalksEveryPageAndSkipsObjectsItAlreadyGave()
    {
        var (first, second, third) = (ChargeWithId("charge000001"), ChargeWithId("charge000002"), ChargeWithId("charge000003"));
        var http = new MockHttp()
            .Json(ListResponse([first, second], currentPage: 1, lastPage: 2, perPage: 2, total: 4))
            .Json(ListResponse([second, third], currentPage: 2, lastPage: 2, perPage: 2, total: 4));
        var options = new ChargeListOptions { Status = ChargeStatus.Paid, PerPage = 2 };
        var ids = new List<string>();

        await foreach (var charge in Client(http).Charges.IteratorAsync(options))
        {
            ids.Add(charge.Id);
        }

        Assert.Equal(["charge000001", "charge000002", "charge000003"], ids);
        Assert.Equal(2, http.Requests.Count);
        Assert.Equal("status=paid&per_page=2", http.Requests[0].Query);
        Assert.Equal("status=paid&per_page=2&page=2", http.Requests[1].Query);
        Assert.Null(options.Page);
    }

    [Fact]
    public async Task APageIsWalkedWithForeachAndFetchesTheNextOne()
    {
        var http = new MockHttp()
            .Json(ListResponse([Fixture("charge")], currentPage: 1, lastPage: 2, perPage: 1, total: 2))
            .Json(ListResponse([ChargeWithId("charge000002")], currentPage: 2, lastPage: 2, perPage: 1, total: 2));

        var page = await Client(http).Charges.ListAsync(new ChargeListOptions { PerPage = 1, Page = 1 });

        Assert.Equal(["k3v9x2m7q8wz"], page.Select(charge => charge.Id));
        Assert.True(page.HasMore);
        Assert.False(page.IsEmpty);
        Assert.Single(page);

        var next = await page.NextPageAsync();

        Assert.NotNull(next);
        Assert.Equal("charge000002", Assert.IsType<Charge>(next[0]).Id);
        Assert.False(next.HasMore);
        Assert.Equal("per_page=1&page=2", http.LastRequest.Query);
    }

    [Fact]
    public async Task TheLastPageHasNoNextPage()
    {
        var http = new MockHttp().Json(ListResponse([Fixture("charge")]));

        var page = await Client(http).Charges.ListAsync();

        Assert.False(page.HasMore);
        Assert.Null(await page.NextPageAsync());
        Assert.Single(http.Requests);
    }

    [Fact]
    public async Task TheIteratorStopsOnAPageThatDoesNotMoveForward()
    {
        var http = new MockHttp()
            .Json(ListResponse([Fixture("charge")], currentPage: 1, lastPage: 3))
            .Json(ListResponse([ChargeWithId("charge000002")], currentPage: 1, lastPage: 3));
        var ids = new List<string>();

        await foreach (var charge in Client(http).Charges.IteratorAsync())
        {
            ids.Add(charge.Id);
        }

        Assert.Equal(["k3v9x2m7q8wz"], ids);
        Assert.Equal(2, http.Requests.Count);
    }

    [Fact]
    public async Task AListWithoutPageNumbersIsOnePage()
    {
        var http = new MockHttp().Json("""{"data":[{"id":"a"},{"id":"b"},"not an object"]}""");

        var page = await Client(http).Charges.ListAsync();

        Assert.Equal(2, page.Count);
        Assert.Equal(1, page.CurrentPage);
        Assert.Equal(1, page.LastPage);
        Assert.Equal(2, page.PerPage);
        Assert.Equal(2, page.Total);
    }

    [Fact]
    public async Task OnlyThePropertiesThatAreSetAreSentWithNullsIncluded()
    {
        var http = new MockHttp().Json(Data(Fixture("payment_link"))).Json(Data(Fixture("payment_link")));
        var client = Client(http);

        await client.PaymentLinks.UpdateAsync("p4n8r2t6y1ua", new PaymentLinkUpdateOptions());

        Assert.Equal("{}", http.LastRequest.Body);

        var options = new PaymentLinkUpdateOptions { Title = "First", Description = "Kept" };
        options.Title = "Second";
        options.Description = null;
        await client.PaymentLinks.UpdateAsync("p4n8r2t6y1ua", options);

        Assert.Equal("""{"title":"Second","description":null}""", http.LastRequest.Body);
        Assert.Equal("Second", options.Title);
        Assert.Null(options.Description);
        Assert.Null(options.MaxPayments);
    }

    [Fact]
    public async Task AFieldTheLibraryHasNoPropertyForIsSentAsGiven()
    {
        var http = new MockHttp().Json(Data(Fixture("charge")), 201).Json(ListResponse([]));
        var client = Client(http);
        var options = new ChargeCreateOptions { Asset = Asset.BTC, Amount = 1m, Currency = Currency.USD };
        options.AddExtraParam("future_field", new Dictionary<string, object?> { ["enabled"] = true, ["limit"] = 3, ["ratio"] = 0.5, ["when"] = new DateOnly(2027, 1, 2) });

        await client.Charges.CreateAsync(options);

        AssertJson("""{"asset":"BTC","amount":"1","currency":"USD","future_field":{"enabled":true,"limit":3,"ratio":0.5,"when":"2027-01-02"}}""", http.LastRequest.Body);

        var listOptions = new ChargeListOptions { Status = ChargeStatus.Paid };
        listOptions.AddExtraParam("tags", new[] { "a", "b" });
        listOptions.AddExtraParam("filter", new Dictionary<string, object?> { ["min"] = 1.5m, ["test"] = false });
        await client.Charges.ListAsync(listOptions);

        Assert.Equal(
            new Dictionary<string, string> { ["status"] = "paid", ["tags[0]"] = "a", ["tags[1]"] = "b", ["filter[min]"] = "1.5", ["filter[test]"] = "0" },
            http.LastRequest.QueryParameters);
        Assert.Throws<InvalidArgumentException>(() => listOptions.AddExtraParam(" ", 1));
    }

    [Fact]
    public async Task DecimalsAreSentAsStringsWithEveryPlaceAndNoExponent()
    {
        var http = new MockHttp().Json(Data(Fixture("charge")), 201);

        await Client(http).Charges.RecordRefundAsync("k3v9x2m7q8wz", new RefundOptions { Amount = 0.000000000000000001m, Reason = "Zoë’s <refund> & \"more\"" });

        Assert.Equal("""{"amount":"0.000000000000000001","reason":"Zoë’s <refund> & \"more\""}""", http.LastRequest.Body);
    }

    [Fact]
    public async Task AComputedAmountIsSentWithoutTheTrailingZerosTheApiWouldCountAsPlaces()
    {
        var http = new MockHttp().Json(Data(Fixture("charge")), 201).Json(Data(Fixture("coupon")), 201).Json(ListResponse([]));
        var client = Client(http);
        var total = 49.90m * 1.20m;

        Assert.Equal("59.8800", total.ToString(System.Globalization.CultureInfo.InvariantCulture));

        await client.Charges.CreateAsync(new ChargeCreateOptions { Asset = Asset.BTC, Amount = total, Currency = Currency.EUR });

        Assert.Equal("""{"asset":"BTC","amount":"59.88","currency":"EUR"}""", http.LastRequest.Body);

        await client.Coupons.CreateAsync(new CouponCreateOptions { Code = "TEN", PercentOff = 10.000m, MinAmount = 100.10m });

        Assert.Equal("""{"code":"TEN","percent_off":"10","min_amount":"100.1"}""", http.LastRequest.Body);

        var options = new ChargeListOptions();
        options.AddExtraParam("min", 1.500m);
        await client.Charges.ListAsync(options);

        Assert.Equal("min=1.5", http.LastRequest.Query);
    }

    [Fact]
    public void AResourceIsReadOnlyFromAJsonObject()
    {
        Assert.Throws<InvalidArgumentException>(() => new Charge(Element(Json("""["not","an","object"]"""))));
    }

    /// <summary>The charge fixture with another id.</summary>
    private static JsonObject ChargeWithId(string id) => Fixture("charge").With(("id", id));
}
