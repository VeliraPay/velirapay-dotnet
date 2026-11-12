using System.Globalization;
using VeliraPay.Tests.Support;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

public sealed class ChargeTests
{
    [Fact]
    public async Task AChargeIsCreated()
    {
        var http = new MockHttp().Json(Data(Fixture("charge")), 201);

        var charge = await Client(http).Charges.CreateAsync(new ChargeCreateOptions
        {
            Asset = Asset.BTC,
            Amount = 150.00m,
            Currency = Currency.EUR,
            Metadata = new() { ["order_id"] = "1042" },
        }, new RequestOptions { IdempotencyKey = "order-1042" });

        var request = http.LastRequest;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/charges", request.Path);
        Assert.Equal("application/json", request.Header("Content-Type"));
        Assert.Equal("order-1042", request.Header("Idempotency-Key"));
        Assert.Equal("""{"asset":"BTC","amount":"150","currency":"EUR","metadata":{"order_id":"1042"}}""", request.Body);
        Assert.Equal("k3v9x2m7q8wz", charge.Id);
    }

    [Fact]
    public async Task AChargeIsCreatedWithEverythingAlreadyKnown()
    {
        var http = new MockHttp().Json(Data(Fixture("charge").With(("success_url", "https://example.com/orders/1042"), ("cancel_url", "https://example.com/cart"), ("success_message", "Thank you."))), 201);

        var charge = await Client(http).Charges.CreateAsync(new ChargeCreateOptions
        {
            Asset = "BTC",
            Amount = 150.00m,
            Currency = "EUR",
            Description = "Pro plan, 1 year",
            CustomFields = [new CustomFieldOptions { Label = "Company", Value = "Analytical Engines" }],
            SuccessUrl = "https://example.com/orders/1042",
            CancelUrl = "https://example.com/cart",
            SuccessMessage = "Thank you.",
            SendEmail = true,
        });

        var sent = http.LastRequest.Json!;
        AssertJson("""[{"label":"Company","value":"Analytical Engines"}]""", sent["custom_fields"]!.ToJsonString());
        Assert.True(sent["send_email"]!.GetValue<bool>());
        Assert.Equal("Pro plan, 1 year", sent["description"]!.GetValue<string>());
        Assert.Equal("https://example.com/orders/1042", charge.SuccessUrl);
        Assert.Equal("https://example.com/cart", charge.CancelUrl);
        Assert.Equal("Thank you.", charge.SuccessMessage);
    }

    [Fact]
    public async Task AChargeCanBeEmailedAcceptedAndPaidBySimulation()
    {
        var http = new MockHttp();
        var client = Client(http);

        http.Json(Data(Fixture("charge")));
        await client.Charges.SendAsync("k3v9x2m7q8wz");
        Assert.Equal(HttpMethod.Post, http.LastRequest.Method);
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/send", http.LastRequest.Path);

        http.Json(Data(Fixture("charge")));
        await client.Charges.AcceptAsync("k3v9x2m7q8wz");
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/accept", http.LastRequest.Path);

        http.Json(Data(Fixture("charge")));
        await client.Charges.SimulateAsync("k3v9x2m7q8wz");
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/simulate", http.LastRequest.Path);
        Assert.Null(http.LastRequest.Body);

        http.Json(Data(Fixture("charge")));
        await client.Charges.SimulateAsync("k3v9x2m7q8wz", new ChargeSimulateOptions());
        Assert.Null(http.LastRequest.Body);

        http.Json(Data(Fixture("charge")));
        await client.Charges.SimulateAsync("k3v9x2m7q8wz", new ChargeSimulateOptions { Amount = 0.001m });
        Assert.Equal("""{"amount":"0.001"}""", http.LastRequest.Body);
    }

    [Fact]
    public async Task AChargeIsReadIntoTypedProperties()
    {
        var http = new MockHttp().Json(Data(Fixture("charge")));

        var charge = await Client(http).Charges.RetrieveAsync("k3v9x2m7q8wz");

        Assert.Equal(HttpMethod.Get, http.LastRequest.Method);
        Assert.Equal("/v1/charges/k3v9x2m7q8wz", http.LastRequest.Path);
        Assert.Equal(ChargeStatus.Paid, charge.Status);
        Assert.True(charge.IsPaid);
        Assert.False(charge.IsOpen);
        Assert.False(charge.IsPending || charge.IsUnderpaid || charge.IsExpired || charge.IsCanceled);
        Assert.Equal("p4n8r2t6y1ua", charge.PaymentLink);
        Assert.Null(charge.PaymentLinkTitle);
        Assert.Null(charge.Invoice);
        Assert.Null(charge.InvoiceNumber);
        Assert.Equal("Pro plan, 1 year", charge.Description);
        Assert.Equal(150.00m, charge.FiatAmount);
        Assert.Equal("150.00", charge.FiatAmount.ToString(CultureInfo.InvariantCulture));
        Assert.Equal("EUR", charge.FiatCurrency);
        Assert.Null(charge.Coupon);
        Assert.Null(charge.DiscountAmount);
        Assert.Equal(Asset.BTC, charge.Asset);
        Assert.Equal(0.0025m, charge.AssetAmount);
        Assert.Equal(0.0025m, charge.ReceivedAmount);
        Assert.Equal(0m, charge.RemainingAmount);
        Assert.False(charge.Overpaid);
        Assert.Equal(0.0005m, charge.RefundedAmount);
        var refund = Assert.Single(charge.Refunds);
        Assert.Equal(0.0005m, refund.Amount);
        Assert.Equal("Partial refund", refund.Reason);
        Assert.StartsWith("c1f0e2d3", refund.Txid, StringComparison.Ordinal);
        Assert.StartsWith("https://mempool.space/tx/", refund.ExplorerUrl, StringComparison.Ordinal);
        Assert.Equal(Utc("2026-09-21T14:30:00Z"), refund.CreatedAt);
        Assert.Equal(60000m, charge.ExchangeRate);
        Assert.Equal("bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh", charge.DepositAddress);
        Assert.Null(charge.NetworkSurcharge);
        Assert.Null(charge.PaymentRequest);
        var transaction = Assert.Single(charge.Transactions);
        Assert.Equal(0.0025m, transaction.Amount);
        Assert.Equal(3, transaction.Confirmations);
        Assert.Equal(2, transaction.RequiredConfirmations);
        Assert.True(transaction.Credited);
        Assert.StartsWith("https://mempool.space/tx/", transaction.ExplorerUrl, StringComparison.Ordinal);
        Assert.Equal(Utc("2026-09-20T09:58:12Z"), transaction.SeenAt);
        Assert.Equal(["charge.created", "charge.payment_detected", "charge.paid"], charge.Timeline.Select(entry => entry.Event));
        Assert.Equal(Utc("2026-09-20T10:05:41Z"), charge.Timeline[2].Time);
        Assert.Equal("ada@example.com", charge.CustomerEmail);
        Assert.Equal("Ada Lovelace", charge.CustomerName);
        Assert.Equal("ada@example.com", charge.Customer.Email);
        Assert.Equal("Ada Lovelace", charge.Customer.Name);
        Assert.Equal("203.0.113.7", charge.Customer.IpAddress);
        Assert.StartsWith("Mozilla/5.0 (Macintosh;", charge.Customer.UserAgent, StringComparison.Ordinal);
        Assert.Equal("cus_1042", charge.Customer.Reference);
        Assert.Equal("+44 20 7946 0958", charge.Customer.Phone);
        Assert.Equal("GB", charge.Customer.Country);
        Assert.Equal(7, charge.Customer.Metadata["orders"].GetInt32());
        Assert.True(charge.Customer.Metadata["verified"].GetBoolean());
        Assert.Equal([new CustomFieldAnswer("Company", "Analytical Engines Ltd")], charge.CustomFields);
        Assert.Equal("1042", Assert.Single(charge.Metadata).Value.GetString());
        Assert.Equal("https://velirapay.com/c/k3v9x2m7q8wz", charge.CheckoutUrl);
        Assert.Equal("https://velirapay.com/c/k3v9x2m7q8wz/receipt", charge.ReceiptUrl);
        Assert.Equal(Utc("2026-09-20T10:25:00Z"), charge.ExpiresAt);
        Assert.Equal(Utc("2026-09-20T10:05:41Z"), charge.PaidAt);
        Assert.Equal(Utc("2026-09-20T09:55:00Z"), charge.CreatedAt);
    }

    [Fact]
    public void AChargeWithANetworkSurchargeOrALightningInvoiceSaysSo()
    {
        var tron = new Charge(Element(Fixture("charge").With(("asset", "USDT_TRON"), ("asset_amount", "52.50"), ("network_surcharge", "2.50"))));
        var lightning = new Charge(Element(Fixture("charge").With(("asset", "BTC_LIGHTNING"), ("payment_request", "lnbc25u1p..."), ("deposit_address", "9f86d081884c7d65"))));

        Assert.Equal(2.50m, tron.NetworkSurcharge);
        Assert.Equal(52.50m, tron.AssetAmount);
        Assert.Equal(Asset.BTC_LIGHTNING, lightning.Asset);
        Assert.Equal("lnbc25u1p...", lightning.PaymentRequest);
        Assert.Equal("9f86d081884c7d65", lightning.DepositAddress);
    }

    [Fact]
    public async Task TheCustomersDetailsAreSentWithANewCharge()
    {
        var http = new MockHttp().Json(Data(Fixture("charge")), 201);

        await Client(http).Charges.CreateAsync(new ChargeCreateOptions
        {
            Asset = Asset.BTC,
            Amount = 150.00m,
            Currency = Currency.EUR,
            Customer = new CustomerOptions
            {
                Email = "ada@example.com",
                IpAddress = "203.0.113.7",
                UserAgent = "Mozilla/5.0",
                Reference = "cus_1042",
                Country = "GB",
                Metadata = new() { ["orders"] = 7 },
            },
        });

        AssertJson(
            """{"email":"ada@example.com","ip_address":"203.0.113.7","user_agent":"Mozilla/5.0","reference":"cus_1042","country":"GB","metadata":{"orders":7}}""",
            http.LastRequest.Json!["customer"]!.ToJsonString());
    }

    [Fact]
    public async Task ACouponIsAppliedToANewCharge()
    {
        var http = new MockHttp().Json(Data(Fixture("charge").With(("fiat_amount", "135.00"), ("coupon", "SPRING10"), ("discount_amount", "15.00"))), 201);

        var charge = await Client(http).Charges.CreateAsync(new ChargeCreateOptions { PaymentLink = "p4n8r2t6y1ua", Asset = Asset.BTC, Coupon = "spring10" });

        AssertJson("""{"payment_link":"p4n8r2t6y1ua","asset":"BTC","coupon":"spring10"}""", http.LastRequest.Body);
        Assert.Equal(135.00m, charge.FiatAmount);
        Assert.Equal("SPRING10", charge.Coupon);
        Assert.Equal(15.00m, charge.DiscountAmount);
    }

    [Fact]
    public async Task AChargeSentWithoutACustomerObjectTakesItFromTheOlderFields()
    {
        var http = new MockHttp().Json(Data(Fixture("charge").Without("customer")));

        var charge = await Client(http).Charges.RetrieveAsync("k3v9x2m7q8wz");

        Assert.Equal("ada@example.com", charge.Customer.Email);
        Assert.Equal("Ada Lovelace", charge.Customer.Name);
        Assert.Null(charge.Customer.IpAddress);
        Assert.Empty(charge.Customer.Metadata);
    }

    [Fact]
    public void AChargeBuiltWithoutACustomerTakesItFromTheOlderFields()
    {
        var charge = new Charge(Element(Json("""{"id":"k3v9x2m7q8wz","status":"pending","customer_email":"ada@example.com","customer_name":"Ada Lovelace"}""")));

        Assert.True(charge.IsPending);
        Assert.True(charge.IsOpen);
        Assert.Equal("ada@example.com", charge.Customer.Email);
        Assert.Equal("Ada Lovelace", charge.Customer.Name);
        Assert.Null(charge.Customer.IpAddress);
        Assert.Equal("""{"email":"ada@example.com","name":"Ada Lovelace"}""", charge.Customer.ToJson());
    }

    [Fact]
    public void ACustomerObjectWinsOverTheOlderFields()
    {
        var charge = new Charge(Element(Json("""{"customer_email":"old@example.com","customer_name":"Old","customer":{"email":"ada@example.com","name":null,"phone":"+44"}}""")));

        Assert.Equal("ada@example.com", charge.Customer.Email);
        Assert.Null(charge.Customer.Name);
        Assert.Equal("+44", charge.Customer.Phone);
        Assert.Equal("old@example.com", charge.CustomerEmail);
    }

    [Fact]
    public async Task AttributesTheLibraryDoesNotKnowAreKept()
    {
        var attributes = Fixture("charge").With(("status", "refunding"), ("network_fee", "0.00001"));
        var http = new MockHttp().Json(Data(attributes));

        var charge = await Client(http).Charges.RetrieveAsync("k3v9x2m7q8wz");

        Assert.Equal("refunding", charge.Status);
        Assert.False(charge.IsPaid);
        Assert.Equal("0.00001", charge.Get("network_fee")?.GetString());
        Assert.Null(charge.Get("nothing"));
        Assert.Equal(JsonValueKind.Null, charge.Get("invoice")?.ValueKind);
        AssertJson(attributes, charge.ToJson());
        Assert.Equal(JsonValueKind.Object, charge.RawJson.ValueKind);
    }

    [Fact]
    public void AmountsKeepEveryDecimalPlaceTheyWereSentWith()
    {
        var charge = new Charge(Element(Json("""{"asset_amount":"0.002500000000000000","exchange_rate":"60000.000000000000000000","received_amount":0.0025,"remaining_amount":"not a number"}""")));

        Assert.Equal("0.002500000000000000", charge.AssetAmount.ToString(CultureInfo.InvariantCulture));
        Assert.Equal("60000.000000000000000000", charge.ExchangeRate.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(0.0025m, charge.ReceivedAmount);
        Assert.Equal(0m, charge.RemainingAmount);
    }

    [Fact]
    public async Task IdsAreEscapedIntoPaths()
    {
        var http = new MockHttp().Json(Data(Fixture("charge"))).Json(Data(Fixture("charge")));

        await Client(http).Charges.RetrieveAsync("../account");

        Assert.Equal("/v1/charges/..%2Faccount", http.LastRequest.Path);

        await Client(http).Charges.RetrieveAsync("k3v 9x?#");

        Assert.Equal("/v1/charges/k3v%209x%3F%23", http.LastRequest.Path);
    }

    [Fact]
    public async Task AnEmptyIdIsRefusedBeforeARequestIsSent()
    {
        var http = new MockHttp();

        var exception = await Assert.ThrowsAsync<InvalidArgumentException>(() => Client(http).Charges.CancelAsync(string.Empty));

        Assert.Equal("The id in /v1/charges/{id}/cancel cannot be empty.", exception.Message);
        await Assert.ThrowsAsync<InvalidArgumentException>(() => Client(http).Charges.RetrieveAsync(" "));
        await Assert.ThrowsAsync<InvalidArgumentException>(() => Client(http).Charges.RetrieveAsync(null!));
        Assert.Empty(http.Requests);
    }

    [Fact]
    public async Task AnIdThatAUrlWouldReadAsADirectoryIsRefused()
    {
        var http = new MockHttp();

        var exception = await Assert.ThrowsAsync<InvalidArgumentException>(() => Client(http).Charges.RetrieveAsync(".."));

        Assert.Equal("The id in /v1/charges/{id} cannot be \"..\".", exception.Message);
        await Assert.ThrowsAsync<InvalidArgumentException>(() => Client(http).WebhookEndpoints.RetryDeliveryAsync("12", "."));
        Assert.Empty(http.Requests);
    }

    [Fact]
    public async Task ChargesAreListedWithFilters()
    {
        var http = new MockHttp().Json(ListResponse([Fixture("charge")], currentPage: 2, lastPage: 3, perPage: 1, total: 3));

        var page = await Client(http).Charges.ListAsync(new ChargeListOptions
        {
            Status = ChargeStatus.Paid,
            PaymentLink = "p4n8r2t6y1ua",
            CreatedFrom = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            CreatedTo = new DateTimeOffset(2026, 10, 31, 23, 59, 59, TimeSpan.FromHours(2)),
            Page = 2,
            PerPage = 1,
        });

        Assert.Equal("/v1/charges", http.LastRequest.Path);
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["status"] = "paid",
                ["payment_link"] = "p4n8r2t6y1ua",
                ["created_from"] = "2026-10-01T00:00:00+00:00",
                ["created_to"] = "2026-10-31T23:59:59+02:00",
                ["page"] = "2",
                ["per_page"] = "1",
            },
            http.LastRequest.QueryParameters);
        Assert.Contains("created_from=2026-10-01T00%3A00%3A00%2B00%3A00", http.LastRequest.Query, StringComparison.Ordinal);
        Assert.IsType<Charge>(Assert.Single(page.Data));
        Assert.Equal(2, page.CurrentPage);
        Assert.Equal(3, page.LastPage);
        Assert.Equal(1, page.PerPage);
        Assert.Equal(3, page.Total);
        Assert.True(page.HasMore);
    }

    [Fact]
    public async Task AChargeIsCanceled()
    {
        var http = new MockHttp().Json(Data(Fixture("charge").With(("status", "canceled"))));

        var charge = await Client(http).Charges.CancelAsync("k3v9x2m7q8wz", new RequestOptions { IdempotencyKey = "cancel-1" });

        var request = http.LastRequest;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/cancel", request.Path);
        Assert.Null(request.Body);
        Assert.Null(request.Header("Content-Type"));
        Assert.Equal("cancel-1", request.Header("Idempotency-Key"));
        Assert.True(charge.IsCanceled);
    }

    [Fact]
    public async Task ARefundIsRecorded()
    {
        var http = new MockHttp().Json(Data(Fixture("charge")), 201);

        await Client(http).Charges.RecordRefundAsync("k3v9x2m7q8wz", new RefundOptions { Amount = 0.0005m, Txid = "c1f0e2d3", Reason = "Partial refund" });

        var request = http.LastRequest;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/refunds", request.Path);
        Assert.Equal("""{"amount":"0.0005","txid":"c1f0e2d3","reason":"Partial refund"}""", request.Body);
    }
}
