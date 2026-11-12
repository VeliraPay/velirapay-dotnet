using VeliraPay.Tests.Support;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

/// <summary>The coins on offer, events and payouts.</summary>
public sealed class CatalogueTests
{
    private static readonly JsonObject PayoutJson = Json("""
        {
            "id": "k3v9x2m7q8wz", "object": "payout", "charge": "k3v9x2m7q8wz", "status": "sent", "asset": "USDT",
            "gross_amount": "11.014175", "fee_percent": "3", "fee_amount": "0.330425", "fee_fiat_amount": "0.33", "fiat_currency": "USD", "exchange_rate": "0.9987",
            "net_amount": "10.68375", "network_fee": "0.081611", "sent_amount": "10.602139",
            "payout_address": "0x7099", "payout_txid": "0x9a4f", "explorer_url": "https://etherscan.io/tx/0x9a4f",
            "attempts": 1, "last_error": null, "next_attempt_at": null,
            "sent_at": "2026-11-02T20:58:00.000000Z", "confirmed_at": "2026-11-02T21:00:04.000000Z", "created_at": "2026-11-02T20:57:00.000000Z"
        }
        """).AsObject();

    [Fact]
    public async Task TheCoinsOnOfferAreReadWithTheirMinimums()
    {
        var http = new MockHttp().Json("""
            {
                "data": [
                    {"id": "BTC", "object": "asset", "name": "Bitcoin", "ticker": "BTC", "network": "Bitcoin", "contract_address": null, "decimals": 8, "required_confirmations": 2,
                     "confirmation_wait": "about 20 minutes", "accepted": true, "currency": "EUR", "exchange_rate": "68527.48", "minimum_amount": null, "minimum_price": "0.01", "network_surcharge": null},
                    {"id": "USDT", "object": "asset", "name": "Tether USD", "ticker": "USDT", "network": "Ethereum (ERC-20)", "contract_address": "0xdAC17F958D2ee523a2206206994597C13D831ec7",
                     "decimals": 6, "required_confirmations": 6, "confirmation_wait": "about a minute", "accepted": false, "currency": "EUR", "exchange_rate": "0.8709",
                     "minimum_amount": "10", "minimum_price": "8.71", "network_surcharge": null},
                    {"id": "NEWCOIN", "object": "asset", "name": "New", "ticker": "NEW", "network": "New", "contract_address": null, "decimals": 18, "required_confirmations": 1,
                     "confirmation_wait": "under a minute", "accepted": null, "currency": "EUR", "exchange_rate": null, "minimum_amount": null, "minimum_price": "0.01", "network_surcharge": "2.50"}
                ],
                "meta": {"mode": "live", "currencies": ["USD", "EUR"]}
            }
            """);

        var page = await Client(http).Assets.ListAsync(new AssetListOptions { Currency = Currency.EUR });

        Assert.Equal("/v1/assets", http.LastRequest.Path);
        Assert.Equal("currency=EUR", http.LastRequest.Query);
        Assert.Equal(3, page.Count);
        Assert.False(page.HasMore);
        Assert.Equal(Mode.Live, page.Mode);
        Assert.Equal([Currency.USD, Currency.EUR], page.Currencies);
        Assert.Equal(JsonValueKind.Array, page.Meta.GetProperty("currencies").ValueKind);

        var bitcoin = page[0];
        Assert.Equal("BTC", bitcoin.Id);
        Assert.Equal(Asset.BTC, bitcoin.Asset);
        Assert.True(bitcoin.Accepted);
        Assert.Null(bitcoin.MinimumAmount);
        Assert.Equal(0.01m, bitcoin.MinimumPrice);
        Assert.Null(bitcoin.ContractAddress);
        Assert.Equal(8, bitcoin.Decimals);
        Assert.Equal(2, bitcoin.RequiredConfirmations);
        Assert.Equal("about 20 minutes", bitcoin.ConfirmationWait);

        var tether = page[1];
        Assert.Equal("Tether USD", tether.Name);
        Assert.Equal("USDT", tether.Ticker);
        Assert.Equal("Ethereum (ERC-20)", tether.Network);
        Assert.Equal("0xdAC17F958D2ee523a2206206994597C13D831ec7", tether.ContractAddress);
        Assert.False(tether.Accepted);
        Assert.Equal("EUR", tether.Currency);
        Assert.Equal(0.8709m, tether.ExchangeRate);
        Assert.Equal(10m, tether.MinimumAmount);
        Assert.Equal(8.71m, tether.MinimumPrice);

        var unknown = page[2];
        Assert.Null(unknown.Asset);
        Assert.Null(unknown.Accepted);
        Assert.Null(unknown.ExchangeRate);
        Assert.Equal(2.50m, unknown.NetworkSurcharge);
    }

    [Fact]
    public async Task AnEventIsRetrievedWithItsCharge()
    {
        var http = new MockHttp().Json(Data(EventJson()));

        var read = await Client(http).Events.RetrieveAsync("9b1f7a3e-2c4d-4e8f-a6b0-1d2c3e4f5a6b");

        Assert.Equal("/v1/events/9b1f7a3e-2c4d-4e8f-a6b0-1d2c3e4f5a6b", http.LastRequest.Path);
        Assert.Equal("9b1f7a3e-2c4d-4e8f-a6b0-1d2c3e4f5a6b", read.Id);
        Assert.Equal("charge.paid", read.Type);
        Assert.True(read.Is(EventType.ChargeUnderpaid, "charge.paid"));
        Assert.False(read.Is(EventType.ChargeExpired));
        Assert.Equal("0.0025", read.Details["amount"].GetString());
        Assert.Equal(Utc("2026-09-20T10:05:41Z"), read.CreatedAt);
        Assert.Equal("k3v9x2m7q8wz", read.Charge?.Id);
        Assert.Null(read.Invoice);
    }

    [Fact]
    public async Task EventsAreListedWithFilters()
    {
        var http = new MockHttp().Json(ListResponse([EventJson()]));

        var page = await Client(http).Events.ListAsync(new EventListOptions { Type = EventType.ChargePaid, Charge = "k3v9x2m7q8wz" });

        Assert.Equal("/v1/events", http.LastRequest.Path);
        Assert.Equal(new Dictionary<string, string> { ["type"] = "charge.paid", ["charge"] = "k3v9x2m7q8wz" }, http.LastRequest.QueryParameters);
        Assert.IsType<Event>(Assert.Single(page));
    }

    [Fact]
    public async Task APayoutIsReadByItsChargeIntoTypedProperties()
    {
        var http = new MockHttp().Json(Data(PayoutJson));

        var read = await Client(http).Payouts.RetrieveAsync("k3v9x2m7q8wz");

        Assert.Equal("/v1/payouts/k3v9x2m7q8wz", http.LastRequest.Path);
        Assert.Equal("k3v9x2m7q8wz", read.Id);
        Assert.Equal("k3v9x2m7q8wz", read.Charge);
        Assert.True(read.IsSent);
        Assert.False(read.IsManual);
        Assert.Equal(PayoutStatus.Sent, read.Status);
        Assert.Equal(Asset.USDT, read.Asset);
        Assert.Equal(11.014175m, read.GrossAmount);
        Assert.Equal(3m, read.FeePercent);
        Assert.Equal(0.330425m, read.FeeAmount);
        Assert.Equal(0.33m, read.FeeFiatAmount);
        Assert.Equal("USD", read.FiatCurrency);
        Assert.Equal(0.9987m, read.ExchangeRate);
        Assert.Equal(10.68375m, read.NetAmount);
        Assert.Equal(0.081611m, read.NetworkFee);
        Assert.Equal(10.602139m, read.SentAmount);
        Assert.Equal("0x7099", read.PayoutAddress);
        Assert.Equal("0x9a4f", read.PayoutTxid);
        Assert.Equal("https://etherscan.io/tx/0x9a4f", read.ExplorerUrl);
        Assert.Equal(1, read.Attempts);
        Assert.Null(read.LastError);
        Assert.Null(read.NextAttemptAt);
        Assert.Equal(Utc("2026-11-02T20:58:00Z"), read.SentAt);
        Assert.Equal(Utc("2026-11-02T21:00:04Z"), read.ConfirmedAt);
        Assert.Equal(Utc("2026-11-02T20:57:00Z"), read.CreatedAt);
    }

    [Fact]
    public async Task PayoutsAreListedWithTheirFilters()
    {
        var http = new MockHttp().Json(new JsonObject { ["data"] = new JsonArray(PayoutJson.DeepClone()), ["meta"] = Json("""{"current_page":1,"last_page":1,"per_page":25,"total":1}""") });

        var page = await Client(http).Payouts.ListAsync(new PayoutListOptions { Status = PayoutStatus.Sent, Asset = Asset.USDT });

        Assert.Equal("/v1/payouts", http.LastRequest.Path);
        Assert.Equal("status=sent&asset=USDT", http.LastRequest.Query);
        Assert.Equal("k3v9x2m7q8wz", page[0].Id);
    }

    [Fact]
    public async Task TheSummaryAddsUpEachCoin()
    {
        var http = new MockHttp().Json("""
            {"data": [{"object": "payout_summary", "asset": "BTC", "payouts": 3, "gross_amount": "0.3", "fee_amount": "0.009", "owed_amount": "0.097",
                       "in_flight_amount": "0", "paid_out_amount": "0.194", "network_fee_amount": "0.0001", "waiting_for_address": 0}],
             "meta": {"mode": "live"}}
            """);

        var client = Client(http);
        var summary = await client.Payouts.SummaryAsync();

        Assert.Equal("/v1/payouts/summary", http.LastRequest.Path);
        Assert.Equal(Mode.Live, client.LastResponse!.Json().GetProperty("meta").GetProperty("mode").GetString());
        var row = Assert.Single(summary);
        Assert.Equal(Asset.BTC, row.Asset);
        Assert.Equal(3, row.Payouts);
        Assert.Equal(0.3m, row.GrossAmount);
        Assert.Equal(0.009m, row.FeeAmount);
        Assert.Equal(0.194m, row.PaidOutAmount);
        Assert.Equal(0m, row.InFlightAmount);
        Assert.Equal(0.097m, row.OwedAmount);
        Assert.Equal(0.0001m, row.NetworkFeeAmount);
        Assert.Equal(0, row.WaitingForAddress);
    }

    /// <summary>An event about the charge fixture.</summary>
    private static JsonObject EventJson() => new()
    {
        ["id"] = "9b1f7a3e-2c4d-4e8f-a6b0-1d2c3e4f5a6b",
        ["object"] = "event",
        ["type"] = "charge.paid",
        ["details"] = Json("""{"amount":"0.0025"}"""),
        ["created_at"] = "2026-09-20T10:05:41.000000Z",
        ["charge"] = Fixture("charge"),
        ["invoice"] = null,
    };
}
