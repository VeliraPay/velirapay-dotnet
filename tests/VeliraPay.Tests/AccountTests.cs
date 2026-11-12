using VeliraPay.Tests.Support;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

public sealed class AccountTests
{
    [Fact]
    public async Task TheAccountIsRetrievedWithTheCoinsItAccepts()
    {
        var http = new MockHttp().Json(Fixture("account"));

        var account = await Client(http).Account.RetrieveAsync();

        Assert.Equal("/v1/account", http.LastRequest.Path);
        Assert.Equal("acme", account.Id);
        Assert.True(account.IsOrganization);
        Assert.Equal("Acme", account.Name);
        Assert.Equal("Acme Inc.", account.DisplayName);
        Assert.Null(account.LogoUrl);
        Assert.Equal("https://acme.example", account.WebsiteUrl);
        Assert.Equal("billing@acme.example", account.SupportEmail);
        Assert.Equal("+442071234567", account.SupportPhone);
        Assert.Equal("#4f46e5", account.BrandColor);
        Assert.Equal("EUR", account.DefaultCurrency);
        Assert.Equal(1m, account.UnderpaymentTolerance);
        Assert.Equal(3m, account.OverpaymentThreshold);
        Assert.Equal(Utc("2026-01-15T10:00:00Z"), account.CreatedAt);
        Assert.Equal(Mode.Test, account.Mode);
        Assert.True(account.IsTestMode);
        Assert.Equal(["BTC", "ETH", "USDC"], account.AcceptedAssets);
    }

    [Fact]
    public async Task TheBusinessBehindTheAccountIsReadIntoTypedProperties()
    {
        var http = new MockHttp().Json(Fixture("account"));

        var business = (await Client(http).Account.RetrieveAsync()).Business;

        Assert.Equal(BusinessType.Company, business.Type);
        Assert.Equal("Acme Holdings Ltd", business.LegalName);
        Assert.Equal("12345678", business.RegistrationNumber);
        Assert.Equal("GB123456789", business.TaxId);
        Assert.Equal("1 Example Street", business.Address.Line1);
        Assert.Null(business.Address.Line2);
        Assert.Equal("London", business.Address.City);
        Assert.Equal("EC1A 1BB", business.Address.PostalCode);
        Assert.Null(business.Address.State);
        Assert.Equal("GB", business.Address.Country);
        Assert.Equal(Industry.Software, business.Industry);
        Assert.Equal("Project management software, billed monthly.", business.ProductDescription);
        Assert.Equal(MonthlyVolume.Under50k, business.MonthlyVolume);
        Assert.Equal("+442071234500", business.Phone);
    }

    [Fact]
    public async Task TheAccountsCheckoutInvoiceAndPaymentSettingsAreRead()
    {
        var http = new MockHttp().Json("""
            {"data": {
                "id": "acme",
                "logo_dark_url": "https://acme.example/logo-dark.png",
                "terms_url": "https://acme.example/terms",
                "refund_policy_url": "https://acme.example/refunds",
                "checkout": {"appearance": "dark", "coin_order": ["BTC", "ETH"], "default_coin": "LTC", "success_url": null, "cancel_url": null, "success_message": "Thank you", "hide_powered_by": true},
                "invoices": {"number_prefix": "BILL", "footer": "Thank you", "show_business_details": true},
                "checkout_locale": "fr",
                "invoice_reminders": true,
                "api_payout_address_changes": true
            }}
            """);

        var account = await Client(http).Account.RetrieveAsync();

        Assert.Equal("https://acme.example/logo-dark.png", account.LogoDarkUrl);
        Assert.Equal("https://acme.example/terms", account.TermsUrl);
        Assert.Equal("https://acme.example/refunds", account.RefundPolicyUrl);
        Assert.NotNull(account.Checkout);
        Assert.Equal("dark", account.Checkout.Appearance);
        Assert.Equal(["BTC", "ETH"], account.Checkout.CoinOrder);
        Assert.Equal("LTC", account.Checkout.DefaultCoin);
        Assert.Null(account.Checkout.SuccessUrl);
        Assert.Null(account.Checkout.CancelUrl);
        Assert.Equal("Thank you", account.Checkout.SuccessMessage);
        Assert.True(account.Checkout.HidePoweredBy);
        Assert.Equal(new AccountInvoices { NumberPrefix = "BILL", Footer = "Thank you", ShowBusinessDetails = true }, account.Invoices);
        Assert.Equal("fr", account.CheckoutLocale);
        Assert.True(account.InvoiceReminders);
        Assert.True(account.ApiPayoutAddressChanges);
    }

    [Fact]
    public async Task MissingAccountSettingsHaveSafeDefaults()
    {
        var http = new MockHttp().Json("""{"data":{"id":"acme"}}""");

        var account = await Client(http).Account.RetrieveAsync();

        Assert.Null(account.LogoDarkUrl);
        Assert.Null(account.TermsUrl);
        Assert.Null(account.RefundPolicyUrl);
        Assert.Null(account.Checkout);
        Assert.Null(account.Invoices);
        Assert.Null(account.CheckoutLocale);
        Assert.False(account.InvoiceReminders);
        Assert.False(account.ApiPayoutAddressChanges);
        Assert.Equal("free", account.Plan);
        Assert.False(account.IsPro);
        Assert.Equal(0m, account.FeePercent);
        Assert.Null(account.Mode);
        Assert.Empty(account.AcceptedAssets);
    }

    [Fact]
    public async Task BusinessDetailsWithheldFromAKeyWithoutAccountWriteReadAsNull()
    {
        var http = new MockHttp().Json("""{"data":{"id":"acme","display_name":"Acme Inc.","business":null},"meta":{"accepted_assets":["BTC"],"mode":"live"}}""");

        var account = await Client(http).Account.RetrieveAsync();

        Assert.Equal(JsonValueKind.Null, account.Get("business")?.ValueKind);
        Assert.Equal("{}", account.Business.ToJson());
        Assert.Null(account.Business.Type);
        Assert.Null(account.Business.LegalName);
        Assert.Null(account.Business.TaxId);
        Assert.Null(account.Business.Address.Country);
        Assert.Null(account.Business.Industry);
        Assert.Null(account.Business.MonthlyVolume);
        Assert.Equal("Acme Inc.", account.DisplayName);
        Assert.Equal(["BTC"], account.AcceptedAssets);
        Assert.False(account.IsTestMode);
    }

    [Fact]
    public async Task BusinessDetailsThatAreUnsetOrUnknownToThisLibraryAreNull()
    {
        var http = new MockHttp().Json("""{"data":{"id":"acme","business":{"type":"cooperative","industry":null}},"meta":{}}""");

        var account = await Client(http).Account.RetrieveAsync();

        Assert.Null(account.SupportPhone);
        Assert.Null(account.Business.Type);
        Assert.Equal("cooperative", account.Business.Get("type")?.GetString());
        Assert.Null(account.Business.Industry);
        Assert.Null(account.Business.MonthlyVolume);
        Assert.Null(account.Business.LegalName);
        Assert.Null(account.Business.Address.Country);
    }

    [Fact]
    public async Task TheAccountAndItsPaymentSettingsAreChanged()
    {
        var data = Fixture("account")["data"]!.AsObject();
        var http = new MockHttp()
            .Json(new JsonObject { ["data"] = data.With(("display_name", "Acme"), ("plan", Json("""{"name":"pro","fee_percent":"0","pro_ends_at":"2026-12-06T00:00:00.000000Z"}"""))), ["meta"] = Json("""{"mode":"live","accepted_assets":["BTC"]}""") })
            .Json(new JsonObject { ["data"] = data.DeepClone(), ["meta"] = Json("""{"mode":"live","accepted_assets":[]}""") });
        var client = Client(http);

        var updated = await client.Account.UpdateAsync(new AccountUpdateOptions
        {
            DisplayName = "Acme",
            WebsiteUrl = null,
            Checkout = new AccountCheckoutOptions { Appearance = "dark", CoinOrder = [Asset.BTC, Asset.ETH] },
            Business = new AccountBusinessOptions { Type = BusinessType.Company, Address = new AddressOptions { Country = "GB" } },
        });

        Assert.Equal(HttpMethod.Patch, http.LastRequest.Method);
        Assert.Equal("/v1/account", http.LastRequest.Path);
        AssertJson(
            """{"display_name":"Acme","website_url":null,"checkout":{"appearance":"dark","coin_order":["BTC","ETH"]},"business":{"type":"company","address":{"country":"GB"}}}""",
            http.LastRequest.Body);
        Assert.Equal("Acme", updated.DisplayName);
        Assert.Equal(["BTC"], updated.AcceptedAssets);
        Assert.True(updated.IsPro);
        Assert.Equal(0m, updated.FeePercent);
        Assert.Equal(Utc("2026-12-06T00:00:00Z"), updated.ProEndsAt);

        var settings = await client.Account.UpdatePaymentSettingsAsync(new PaymentSettingsUpdateOptions { UnderpaymentTolerance = 2m, CheckoutLocale = null });

        Assert.Equal(HttpMethod.Patch, http.LastRequest.Method);
        Assert.Equal("/v1/payment-settings", http.LastRequest.Path);
        Assert.Equal("""{"underpayment_tolerance":"2","checkout_locale":null}""", http.LastRequest.Body);
        Assert.Equal(Mode.Live, settings.Mode);
    }

    [Fact]
    public async Task TheOverviewIsRetrieved()
    {
        var http = new MockHttp().Json("""
            {"data": {
                "object": "overview", "currency": "USD", "volume": "1250.00", "volume_change_percent": 12.5, "sent_volume": "1100.00", "paid_charges": 12,
                "conversion_rate_percent": 60, "pending_charges": 2, "active_payment_links": 3, "open_invoices": 1, "overdue_invoices": 0, "funnel": {"started": 20}
            }}
            """);

        var overview = await Client(http).Overview.RetrieveAsync();

        Assert.Equal("/v1/overview", http.LastRequest.Path);
        Assert.Equal("USD", overview.Currency);
        Assert.Equal(1250.00m, overview.Volume);
        Assert.Equal(12.5m, overview.VolumeChangePercent);
        Assert.Equal(1100.00m, overview.SentVolume);
        Assert.Equal(12, overview.PaidCharges);
        Assert.Equal(60m, overview.ConversionRatePercent);
        Assert.Equal(2, overview.PendingCharges);
        Assert.Equal(3, overview.ActivePaymentLinks);
        Assert.Equal(1, overview.OpenInvoices);
        Assert.Equal(0, overview.OverdueInvoices);
        Assert.Equal(20, overview.Get("funnel")?.GetProperty("started").GetInt32());
        Assert.Equal(20, overview.Funnel.Started);
        Assert.Equal(0, overview.Funnel.Paid);
        Assert.Empty(overview.Funnel.Assets);
        Assert.Equal(new OverviewPeriod(), overview.Period);
        Assert.Empty(overview.Attention.UnderpaidCharges);
        Assert.Empty(overview.Attention.LatePayments);
        Assert.Empty(overview.Attention.PayoutsWaitingForAddress);
        Assert.Equal(0, overview.Attention.FailingWebhookEndpoints);
        Assert.Empty(overview.Assets);
        Assert.Empty(overview.TopPaymentLinks);
        Assert.Equal(new OverviewSetup(), overview.Setup);
        Assert.Empty(overview.Daily);
        Assert.Null(overview.Mode);
    }

    [Fact]
    public async Task TheOverviewsFunnelAttentionCoinsLinksSetupAndDaysAreReadIntoTypedProperties()
    {
        var http = new MockHttp().Json(Fixture("overview"));

        var overview = await Client(http, "vp_live_secret").Overview.RetrieveAsync();

        Assert.Equal("/v1/overview", http.LastRequest.Path);
        Assert.Equal(Mode.Live, overview.Mode);
        Assert.Equal(new OverviewPeriod { From = new DateOnly(2026, 10, 14), To = new DateOnly(2026, 11, 12) }, overview.Period);

        var funnel = overview.Funnel;
        Assert.Equal((20, 12, 2, 5, 1), (funnel.Started, funnel.Paid, funnel.Waiting, funnel.Abandoned, funnel.Incomplete));
        Assert.Equal(
            [
                new OverviewFunnelAsset { Asset = Asset.BTC, Started = 12, Paid = 8, Abandoned = 3 },
                new OverviewFunnelAsset { Asset = Asset.USDT_TRON, Started = 8, Paid = 4, Abandoned = 2 },
            ],
            funnel.Assets);

        var attention = overview.Attention;
        var underpaid = Assert.Single(attention.UnderpaidCharges);
        Assert.Equal(new OverviewChargeAmount { Id = "u7p3a1d9e2rx", Asset = Asset.BTC, Amount = 0.0025m, ReceivedAmount = 0.002m }, underpaid);
        Assert.Equal("0.002000000000000000", underpaid.ReceivedAmount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal([new OverviewChargeAmount { Id = "l4t3p9a2y8mn", Asset = Asset.ETH, Amount = 0.05m, ReceivedAmount = 0.05m }], attention.LatePayments);
        Assert.Equal([new OverviewWaitingPayouts { Asset = Asset.SOL, Count = 2 }], attention.PayoutsWaitingForAddress);
        Assert.Equal(1, attention.FailingWebhookEndpoints);

        Assert.Equal(
            [
                new OverviewAsset { Asset = Asset.BTC, PaidCharges = 8, SharePercent = 66.67m },
                new OverviewAsset { Asset = Asset.USDT_TRON, PaidCharges = 4, SharePercent = 33.33m },
            ],
            overview.Assets);
        Assert.Equal([new OverviewPaymentLink { Id = "p4n8r2t6y1ua", Title = "Pro plan", PaidCharges = 7 }], overview.TopPaymentLinks);
        Assert.Equal(new OverviewSetup { PayoutAddress = true, PaymentLink = true, PaidCharge = true, WebhookEndpoint = false, PublicProfile = true }, overview.Setup);
        Assert.Equal(
            [
                new OverviewDay { Date = new DateOnly(2026, 11, 11), Volume = 0m, SentVolume = 0m, PaidCharges = 0 },
                new OverviewDay { Date = new DateOnly(2026, 11, 12), Volume = 150.00m, SentVolume = 145.50m, PaidCharges = 1 },
            ],
            overview.Daily);
        Assert.Equal("Pro plan", overview.Get("top_payment_links")?[0].GetProperty("title").GetString());
    }

    [Fact]
    public void AnOverviewBuiltWithoutItsMetaHasNoMode()
    {
        var overview = new Overview(Element(Fixture("overview")["data"]!));

        Assert.Null(overview.Mode);
        Assert.Equal(12, overview.PaidCharges);
        Assert.Equal(Mode.Test, new Overview(Element(Fixture("overview")["data"]!), Element(Json("""{"mode":"test"}"""))).Mode);
    }

    [Fact]
    public async Task PayoutAddressesAreListedSetAndRemoved()
    {
        var bitcoin = Json("""{"object":"payout_address","network":"bitcoin","name":"Bitcoin","address":"bc1qexample","assets":["BTC"],"payouts_held_until":null,"updated_at":"2026-10-02T08:14:51.000000Z"}""").AsObject();
        var http = new MockHttp()
            .Json(new JsonObject { ["data"] = new JsonArray(bitcoin.DeepClone(), bitcoin.With(("network", "evm"), ("name", "Ethereum"), ("address", null), ("assets", Json("""["ETH","USDT"]""")))), ["meta"] = Json("""{"mode":"live","api_payout_address_changes":true}""") })
            .Json(Data(bitcoin.With(("payouts_held_until", "2026-11-08T08:00:00.000000Z"))))
            .Json(Data(bitcoin.With(("address", null))));
        var client = Client(http);

        var addresses = await client.PayoutAddresses.ListAsync();

        Assert.Equal("/v1/payout-addresses", http.LastRequest.Path);
        Assert.Equal(2, addresses.Count);
        Assert.Equal("bc1qexample", addresses[0].Address);
        Assert.Equal("bitcoin", addresses[0].Network);
        Assert.Equal("Bitcoin", addresses[0].Name);
        Assert.Equal(Utc("2026-10-02T08:14:51Z"), addresses[0].UpdatedAt);
        Assert.Null(addresses[1].Address);
        Assert.Equal(["ETH", "USDT"], addresses[1].Assets);
        Assert.True(client.LastResponse!.Json().GetProperty("meta").GetProperty("api_payout_address_changes").GetBoolean());

        var updated = await client.PayoutAddresses.UpdateAsync(AddressNetwork.Bitcoin, new PayoutAddressUpdateOptions { Address = "bc1qexample" });

        Assert.Equal(HttpMethod.Put, http.LastRequest.Method);
        Assert.Equal("/v1/payout-addresses/bitcoin", http.LastRequest.Path);
        Assert.Equal("""{"address":"bc1qexample"}""", http.LastRequest.Body);
        Assert.Equal(Utc("2026-11-08T08:00:00Z"), updated.PayoutsHeldUntil);

        Assert.Null((await client.PayoutAddresses.DeleteAsync("bitcoin")).Address);
        Assert.Equal(HttpMethod.Delete, http.LastRequest.Method);
    }

    [Fact]
    public async Task TheActivityLogIsListedWithItsFilters()
    {
        var http = new MockHttp().Json("""
            {"data": [{"id": "5", "object": "activity", "action": "api_key.created", "description": "Created an API key", "actor": "API key \"Bot\"", "subject": "Shop",
                       "properties": {"prefix": "vp_live_aaaa"}, "ip_address": "203.0.113.7", "mode": "live", "created_at": "2026-11-06T09:00:00.000000Z"}],
             "meta": {"current_page": 1, "last_page": 1, "per_page": 25, "total": 1, "mode": "live"}}
            """);

        var page = await Client(http).Activity.ListAsync(new ActivityListOptions { Action = "sensitive", From = new DateOnly(2026, 11, 1), Network = Mode.Live });
        var entry = Assert.Single(page);

        Assert.Equal("/v1/activity", http.LastRequest.Path);
        Assert.Equal("action=sensitive&from=2026-11-01&network=live", http.LastRequest.Query);
        Assert.Equal(Mode.Live, page.Mode);
        Assert.Equal(25, page.Meta.GetProperty("per_page").GetInt32());
        Assert.Empty(page.Currencies);
        Assert.Equal("5", entry.Id);
        Assert.Equal("api_key.created", entry.Action);
        Assert.Equal("Created an API key", entry.Description);
        Assert.Equal("API key \"Bot\"", entry.Actor);
        Assert.Equal("Shop", entry.Subject);
        Assert.Equal("203.0.113.7", entry.IpAddress);
        Assert.Equal(Mode.Live, entry.Mode);
        Assert.Equal("vp_live_aaaa", entry.Properties["prefix"].GetString());
        Assert.Equal(Utc("2026-11-06T09:00:00Z"), entry.CreatedAt);
    }

    [Fact]
    public async Task AProPaymentIsStarted()
    {
        var http = new MockHttp().Json("""
            {"data": {"object": "pro_payment", "months": 1, "price": "9.99", "currency": "USD", "asset": "BTC", "asset_amount": "0.0001",
                      "checkout_url": "https://velirapay.com/c/abc", "expires_at": "2026-11-06T10:00:00.000000Z"},
             "meta": {"started": true}}
            """, 201);

        var payment = await Client(http, "vp_live_secret").Billing.StartProPaymentAsync(new ProPaymentOptions { Asset = Asset.BTC });

        Assert.Equal(HttpMethod.Post, http.LastRequest.Method);
        Assert.Equal("/v1/billing/pro", http.LastRequest.Path);
        Assert.Equal("""{"asset":"BTC"}""", http.LastRequest.Body);
        Assert.Equal(1, payment.Months);
        Assert.Equal("https://velirapay.com/c/abc", payment.CheckoutUrl);
        Assert.Equal(9.99m, payment.Price);
        Assert.Equal("USD", payment.Currency);
        Assert.Equal(Asset.BTC, payment.Asset);
        Assert.Equal(0.0001m, payment.AssetAmount);
        Assert.Equal(Utc("2026-11-06T10:00:00Z"), payment.ExpiresAt);
        Assert.True(payment.Started);
    }
}
