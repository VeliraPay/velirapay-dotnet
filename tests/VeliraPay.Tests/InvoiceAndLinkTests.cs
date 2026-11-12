using VeliraPay.Tests.Support;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

/// <summary>Invoices, payment links and coupons.</summary>
public sealed class InvoiceAndLinkTests
{
    [Fact]
    public async Task AnInvoiceIsCreatedWithItsDueDateAsADay()
    {
        var http = new MockHttp().Json(Data(Fixture("invoice")), 201);

        var invoice = await Client(http).Invoices.CreateAsync(new InvoiceCreateOptions
        {
            CustomerName = "Grace Hopper",
            CustomerEmail = "grace@example.com",
            Currency = Currency.USD,
            Items =
            [
                new InvoiceItemOptions { Description = "Consulting", Quantity = 10, UnitAmount = 100.00m },
                new InvoiceItemOptions { Description = "Hosting", Quantity = 2.5m, UnitAmount = 100.00m },
            ],
            DueAt = new DateOnly(2026, 10, 15),
            SuccessUrl = "https://example.com/orders/1042",
            CancelUrl = "https://example.com/checkout",
            SendEmail = true,
        });

        var request = http.LastRequest;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/invoices", request.Path);
        AssertJson(
            """
            {"customer_name":"Grace Hopper","customer_email":"grace@example.com","currency":"USD",
             "items":[{"description":"Consulting","quantity":"10","unit_amount":"100"},{"description":"Hosting","quantity":"2.5","unit_amount":"100"}],
             "due_at":"2026-10-15","success_url":"https://example.com/orders/1042","cancel_url":"https://example.com/checkout","send_email":true}
            """,
            request.Body);
        Assert.Equal("w7c2h5j9d3kx", invoice.Id);
    }

    [Fact]
    public async Task AnInvoiceIsReadIntoTypedProperties()
    {
        var http = new MockHttp().Json(Data(Fixture("invoice")));

        var invoice = await Client(http).Invoices.RetrieveAsync("w7c2h5j9d3kx");

        Assert.Equal("/v1/invoices/w7c2h5j9d3kx", http.LastRequest.Path);
        Assert.Equal("INV-0042", invoice.Number);
        Assert.Equal(InvoiceStatus.Open, invoice.Status);
        Assert.True(invoice.IsOpen);
        Assert.False(invoice.IsPaid || invoice.IsVoid);
        Assert.False(invoice.Overdue);
        Assert.Equal("Grace Hopper", invoice.CustomerName);
        Assert.Equal("grace@example.com", invoice.CustomerEmail);
        Assert.Equal("Thanks for your business.", invoice.Memo);
        Assert.Equal(1250.00m, invoice.Amount);
        Assert.Equal("USD", invoice.Currency);
        Assert.Equal(2, invoice.Items.Count);
        Assert.Equal(2.5m, invoice.Items[1].Quantity);
        Assert.Equal(100.00m, invoice.Items[1].UnitAmount);
        Assert.Equal(250.00m, invoice.Items[1].Total);
        Assert.Equal(new DateOnly(2026, 10, 15), invoice.DueAt);
        Assert.Equal("https://velirapay.com/i/w7c2h5j9d3kx", invoice.HostedUrl);
        Assert.Equal("https://velirapay.com/i/w7c2h5j9d3kx/document", invoice.DocumentUrl);
        Assert.Equal("https://example.com/orders/1042", invoice.SuccessUrl);
        Assert.Null(invoice.CancelUrl);
        Assert.Equal([new InvoiceCharge("m2q7w4e9r1ty", "expired", "ETH")], invoice.Charges);
        Assert.Equal(Utc("2026-09-21T08:00:00Z"), invoice.SentAt);
        Assert.Null(invoice.PaidAt);
    }

    [Fact]
    public async Task AnInvoiceIsSentAndVoided()
    {
        var http = new MockHttp().Json(Data(Fixture("invoice"))).Json(Data(Fixture("invoice").With(("status", "void"))));
        var client = Client(http);

        await client.Invoices.SendAsync("w7c2h5j9d3kx");

        Assert.Equal(HttpMethod.Post, http.LastRequest.Method);
        Assert.Equal("/v1/invoices/w7c2h5j9d3kx/send", http.LastRequest.Path);

        var invoice = await client.Invoices.VoidAsync("w7c2h5j9d3kx");

        Assert.Equal(HttpMethod.Post, http.LastRequest.Method);
        Assert.Equal("/v1/invoices/w7c2h5j9d3kx/void", http.LastRequest.Path);
        Assert.True(invoice.IsVoid);
    }

    [Fact]
    public async Task InvoicesAreListedByStatusAndOverdueOnesOnTheirOwn()
    {
        var http = new MockHttp().Json(ListResponse([Fixture("invoice")])).Json(ListResponse([]));
        var client = Client(http);

        var page = await client.Invoices.ListAsync(new InvoiceListOptions { Status = InvoiceStatus.Open });

        Assert.Equal("status=open", http.LastRequest.Query);
        Assert.IsType<Invoice>(Assert.Single(page));

        var empty = await client.Invoices.ListAsync(new InvoiceListOptions { Overdue = true, Search = "grace@example.com" });

        Assert.Equal(new Dictionary<string, string> { ["overdue"] = "1", ["search"] = "grace@example.com" }, http.LastRequest.QueryParameters);
        Assert.True(empty.IsEmpty);
    }

    [Fact]
    public async Task APaymentLinkIsCreatedWithItsDatesSentAsTimestamps()
    {
        var http = new MockHttp().Json(Data(Fixture("payment_link")), 201);

        var link = await Client(http).PaymentLinks.CreateAsync(new PaymentLinkCreateOptions
        {
            Title = "Tip jar",
            PricingType = PricingType.Open,
            Currency = Currency.USD,
            SuggestedAmounts = [5.00m, 10.00m, 25.00m],
            AcceptedAssets = [Asset.BTC, "ETH"],
            CustomFields = [new CheckoutQuestionOptions { Label = "Message", Required = false }],
            ExpiresAt = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero),
        });

        var request = http.LastRequest;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/payment-links", request.Path);
        Assert.Equal(
            """{"title":"Tip jar","pricing_type":"open","currency":"USD","suggested_amounts":["5","10","25"],"accepted_assets":["BTC","ETH"],"custom_fields":[{"label":"Message","required":false}],"expires_at":"2026-12-31T23:59:59+00:00"}""",
            request.Body);
        Assert.Equal("p4n8r2t6y1ua", link.Id);
    }

    [Fact]
    public async Task TimesInAnotherOffsetAreSentAsTheSameMomentInUtc()
    {
        var http = new MockHttp().Json(Data(Fixture("payment_link")), 201).Json(Data(Fixture("coupon"))).Json(ListResponse([]));
        var client = Client(http);

        await client.PaymentLinks.CreateAsync(new PaymentLinkCreateOptions
        {
            Title = "Tip jar",
            PricingType = PricingType.Open,
            Currency = Currency.USD,
            ExpiresAt = new DateTimeOffset(2027, 1, 1, 1, 30, 0, TimeSpan.FromHours(3)),
        });

        Assert.Equal("2026-12-31T22:30:00+00:00", http.LastRequest.Json!["expires_at"]!.GetValue<string>());

        await client.Coupons.UpdateAsync("SPRING10", new CouponUpdateOptions { ExpiresAt = new DateTimeOffset(2026, 12, 31, 18, 59, 59, TimeSpan.FromHours(-5)) });

        Assert.Equal("""{"expires_at":"2026-12-31T23:59:59+00:00"}""", http.LastRequest.Body);

        var options = new ChargeListOptions { CreatedFrom = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.FromHours(5.5)) };
        options.AddExtraParam("since", new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.FromHours(-8)));
        await client.Charges.ListAsync(options);

        Assert.Equal(new Dictionary<string, string> { ["created_from"] = "2026-10-31T18:30:00+00:00", ["since"] = "2026-11-02T08:00:00+00:00" }, http.LastRequest.QueryParameters);
    }

    [Fact]
    public async Task APaymentLinkIsReadIntoTypedProperties()
    {
        var http = new MockHttp().Json(Data(Fixture("payment_link")));

        var link = await Client(http).PaymentLinks.RetrieveAsync("p4n8r2t6y1ua");

        Assert.Equal("/v1/payment-links/p4n8r2t6y1ua", http.LastRequest.Path);
        Assert.Equal("Tip jar", link.Title);
        Assert.Equal("Support the project.", link.Description);
        Assert.Equal(PricingType.Open, link.PricingType);
        Assert.False(link.HasFixedAmount);
        Assert.Null(link.Amount);
        Assert.Equal([5.00m, 10.00m, 25.00m], link.SuggestedAmounts);
        Assert.Equal(1.00m, link.MinAmount);
        Assert.Equal(500.00m, link.MaxAmount);
        Assert.Null(link.AcceptedAssets);
        Assert.True(link.IsActive);
        Assert.False(link.IsArchived);
        Assert.Equal("https://example.com/thanks", link.SuccessUrl);
        Assert.True(link.CollectName);
        Assert.False(link.RequireEmail);
        Assert.Equal([new CheckoutQuestion("Message", false)], link.CustomFields);
        Assert.Equal("Thank you!", link.SuccessMessage);
        Assert.Null(link.ExpiresAt);
        Assert.Equal(100, link.MaxPayments);
        Assert.True(link.AcceptingPayments);
        Assert.Equal(12, link.ViewsCount);
        Assert.Equal("https://velirapay.com/pay/p4n8r2t6y1ua", link.CheckoutUrl);
        Assert.Equal(Utc("2026-09-02T08:30:00Z"), link.UpdatedAt);
    }

    [Fact]
    public async Task APaymentLinkIsUpdatedWithNullSentAsIs()
    {
        var http = new MockHttp().Json(Data(Fixture("payment_link").With(("title", "Coffee"))));

        var link = await Client(http).PaymentLinks.UpdateAsync("p4n8r2t6y1ua", new PaymentLinkUpdateOptions { Title = "Coffee", AcceptedAssets = null });

        var request = http.LastRequest;
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("/v1/payment-links/p4n8r2t6y1ua", request.Path);
        Assert.Equal("""{"title":"Coffee","accepted_assets":null}""", request.Body);
        Assert.Equal("Coffee", link.Title);
    }

    [Fact]
    public async Task APaymentLinkIsArchivedAndRestored()
    {
        var http = new MockHttp().Json(Data(Fixture("payment_link").With(("status", "archived")))).Json(Data(Fixture("payment_link")));
        var client = Client(http);

        var archived = await client.PaymentLinks.ArchiveAsync("p4n8r2t6y1ua");

        Assert.Equal(HttpMethod.Post, http.LastRequest.Method);
        Assert.Equal("/v1/payment-links/p4n8r2t6y1ua/archive", http.LastRequest.Path);
        Assert.True(archived.IsArchived);

        var restored = await client.PaymentLinks.RestoreAsync("p4n8r2t6y1ua");

        Assert.Equal(HttpMethod.Delete, http.LastRequest.Method);
        Assert.Equal("/v1/payment-links/p4n8r2t6y1ua/archive", http.LastRequest.Path);
        Assert.True(restored.IsActive);
    }

    [Fact]
    public async Task PaymentLinksAreListed()
    {
        var http = new MockHttp().Json(ListResponse([Fixture("payment_link")]));

        var page = await Client(http).PaymentLinks.ListAsync(new PaymentLinkListOptions { PerPage = 10 });

        Assert.Equal("per_page=10", http.LastRequest.Query);
        Assert.IsType<PaymentLink>(Assert.Single(page));
    }

    [Fact]
    public async Task ACouponIsCreatedWithItsExpirySentAsATimestamp()
    {
        var http = new MockHttp().Json(Data(Fixture("coupon")), 201);

        var coupon = await Client(http).Coupons.CreateAsync(new CouponCreateOptions
        {
            Code = "spring10",
            PercentOff = 10m,
            MaxRedemptions = 100,
            OncePerCustomer = true,
            PaymentLinks = ["p4n8r2t6y1ua"],
            ExpiresAt = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero),
        }, new RequestOptions { IdempotencyKey = "spring-sale" });

        var request = http.LastRequest;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/coupons", request.Path);
        Assert.Equal("spring-sale", request.Header("Idempotency-Key"));
        Assert.Equal(
            """{"code":"spring10","percent_off":"10","max_redemptions":100,"once_per_customer":true,"payment_links":["p4n8r2t6y1ua"],"expires_at":"2026-12-31T23:59:59+00:00"}""",
            request.Body);
        Assert.Equal("SPRING10", coupon.Code);
    }

    [Fact]
    public async Task ACouponIsReadIntoTypedProperties()
    {
        var http = new MockHttp().Json(Data(Fixture("coupon")));

        var coupon = await Client(http).Coupons.RetrieveAsync("spring10");

        Assert.Equal("/v1/coupons/spring10", http.LastRequest.Path);
        Assert.Equal("SPRING10", coupon.Id);
        Assert.Equal("SPRING10", coupon.Code);
        Assert.True(coupon.IsPercentage);
        Assert.Equal(10.00m, coupon.PercentOff);
        Assert.Null(coupon.AmountOff);
        Assert.Null(coupon.Currency);
        Assert.Null(coupon.MinAmount);
        Assert.Equal(100, coupon.MaxRedemptions);
        Assert.Equal(4, coupon.TimesRedeemed);
        Assert.True(coupon.OncePerCustomer);
        Assert.Equal(["p4n8r2t6y1ua"], coupon.PaymentLinks);
        Assert.True(coupon.Active);
        Assert.Equal(Utc("2026-12-31T23:59:59Z"), coupon.ExpiresAt);
        Assert.Equal(Utc("2026-10-15T09:30:00Z"), coupon.UpdatedAt);
    }

    [Fact]
    public async Task AnAmountOffCouponNamesItsCurrency()
    {
        var http = new MockHttp().Json(Data(Fixture("coupon").With(("percent_off", null), ("amount_off", "5.00"), ("currency", "EUR"), ("min_amount", "20.00"))));

        var coupon = await Client(http).Coupons.RetrieveAsync("SPRING10");

        Assert.False(coupon.IsPercentage);
        Assert.Equal(5.00m, coupon.AmountOff);
        Assert.Equal("EUR", coupon.Currency);
        Assert.Equal(20.00m, coupon.MinAmount);
    }

    [Fact]
    public async Task ACouponsLimitsAreUpdated()
    {
        var http = new MockHttp().Json(Data(Fixture("coupon").With(("active", false), ("payment_links", new JsonArray()))));

        var coupon = await Client(http).Coupons.UpdateAsync("SPRING10", new CouponUpdateOptions { Active = false, PaymentLinks = [], ExpiresAt = null });

        var request = http.LastRequest;
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("/v1/coupons/SPRING10", request.Path);
        Assert.Equal("""{"active":false,"payment_links":[],"expires_at":null}""", request.Body);
        Assert.False(coupon.Active);
        Assert.Empty(coupon.PaymentLinks);
    }

    [Fact]
    public async Task CouponsAreListed()
    {
        var http = new MockHttp().Json(ListResponse([Fixture("coupon")]));

        var page = await Client(http).Coupons.ListAsync(new CouponListOptions { PerPage = 10 });

        Assert.Equal("/v1/coupons", http.LastRequest.Path);
        Assert.Equal("per_page=10", http.LastRequest.Query);
        Assert.IsType<Coupon>(Assert.Single(page));
    }

    [Fact]
    public async Task ACouponIsDeleted()
    {
        var http = new MockHttp().Empty();

        await Client(http).Coupons.DeleteAsync("SPRING10");

        Assert.Equal(HttpMethod.Delete, http.LastRequest.Method);
        Assert.Equal("/v1/coupons/SPRING10", http.LastRequest.Path);
        Assert.Null(http.LastRequest.Body);
    }
}
