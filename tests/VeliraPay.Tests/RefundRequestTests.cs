using System.Globalization;
using VeliraPay.Tests.Support;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

/// <summary>Refunds started for a charge, which the customer gives the address of and you send from your own wallet.</summary>
public sealed class RefundRequestTests
{
    private const string AddressUrl = "https://velirapay.com/r/r7d4f1k9m2qa?expires=1791208800&signature=4f9c";

    [Fact]
    public async Task ARefundIsStartedWithItsAmountAsADecimalString()
    {
        var http = new MockHttp().Json(Data(AwaitingAddress()), 201);

        var refundRequest = await Client(http).Charges.StartRefundAsync(
            "k3v9x2m7q8wz",
            new RefundRequestCreateOptions { Amount = 0.00050000m, Reason = "Partial refund" },
            new RequestOptions { IdempotencyKey = "refund-1042-1" });

        var request = http.LastRequest;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/refund-requests", request.Path);
        Assert.Equal("application/json", request.Header("Content-Type"));
        Assert.Equal("refund-1042-1", request.Header("Idempotency-Key"));
        Assert.Equal("""{"amount":"0.0005","reason":"Partial refund"}""", request.Body);
        Assert.Equal(RefundRequestStatus.AwaitingAddress, refundRequest.Status);
        Assert.Equal(AddressUrl, refundRequest.AddressUrl);
        Assert.Null(refundRequest.Address);
        Assert.True(refundRequest.IsOpen);
        Assert.False(refundRequest.IsRefunded);
    }

    [Fact]
    public async Task ARefundIsStartedWithAnAddressTheCustomerAlreadyGave()
    {
        var http = new MockHttp().Json(Data(Fixture("refund_request").With(("status", "awaiting_transfer"), ("address_given_by", "merchant"), ("txid", null), ("detected_txid", null), ("detected_amount", null), ("confirmations", null))), 201);

        var refundRequest = await Client(http).Charges.StartRefundAsync("k3v9x2m7q8wz", new RefundRequestCreateOptions
        {
            Amount = 0.0025m,
            Address = "bc1qar0srrr7xfkvy5l643lydnw9re59gtzzwf5mdq",
        });

        Assert.Equal("""{"amount":"0.0025","address":"bc1qar0srrr7xfkvy5l643lydnw9re59gtzzwf5mdq"}""", http.LastRequest.Body);
        Assert.Equal(RefundRequestStatus.AwaitingTransfer, refundRequest.Status);
        Assert.Equal("merchant", refundRequest.AddressGivenBy);
        Assert.Null(refundRequest.DetectedTxid);
        Assert.Null(refundRequest.Confirmations);
        Assert.True(refundRequest.IsOpen);
    }

    [Fact]
    public async Task ARefundRequestIsReadIntoTypedProperties()
    {
        var http = new MockHttp().Json(Data(Fixture("refund_request")));

        var refundRequest = await Client(http).Charges.RetrieveRefundRequestAsync("k3v9x2m7q8wz", "r7d4f1k9m2qa");

        Assert.Equal(HttpMethod.Get, http.LastRequest.Method);
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/refund-requests/r7d4f1k9m2qa", http.LastRequest.Path);
        Assert.Null(http.LastRequest.Body);
        Assert.Equal("r7d4f1k9m2qa", refundRequest.Id);
        Assert.Equal("k3v9x2m7q8wz", refundRequest.Charge);
        Assert.Equal(RefundRequestStatus.Confirming, refundRequest.Status);
        Assert.Equal("0.0005", refundRequest.Amount.ToString(CultureInfo.InvariantCulture));
        Assert.Equal("Partial refund", refundRequest.Reason);
        Assert.Equal("bc1qar0srrr7xfkvy5l643lydnw9re59gtzzwf5mdq", refundRequest.Address);
        Assert.Equal("customer", refundRequest.AddressGivenBy);
        Assert.Null(refundRequest.AddressUrl);
        Assert.Equal(Utc("2026-10-05T14:00:00Z"), refundRequest.LinkExpiresAt);
        Assert.Equal(Utc("2026-09-21T14:00:02Z"), refundRequest.EmailedAt);
        Assert.StartsWith("c1f0e2d3", refundRequest.Txid, StringComparison.Ordinal);
        Assert.Equal(refundRequest.Txid, refundRequest.DetectedTxid);
        Assert.Equal(0.0005m, refundRequest.DetectedAmount);
        Assert.Equal(1, refundRequest.Confirmations);
        Assert.Null(refundRequest.Refund);
        Assert.Equal(Utc("2026-09-21T14:00:00Z"), refundRequest.CreatedAt);
        Assert.Null(refundRequest.CompletedAt);
        Assert.Null(refundRequest.CanceledAt);
        Assert.True(refundRequest.IsOpen);
        Assert.False(refundRequest.IsRefunded);
        Assert.Equal("refund_request", refundRequest.Get("object")?.GetString());
    }

    [Fact]
    public void ARecordedRefundRequestCarriesItsRefundAndItsCreditNote()
    {
        var refundRequest = new RefundRequest(Element(Fixture("refund_request").With(
            ("status", "refunded"),
            ("confirmations", 2),
            ("completed_at", "2026-09-21T14:30:00.000000Z"),
            ("refund", Fixture("charge")["refunds"]![0]))));

        Assert.True(refundRequest.IsRefunded);
        Assert.False(refundRequest.IsOpen);
        Assert.Equal(Utc("2026-09-21T14:30:00Z"), refundRequest.CompletedAt);
        var refund = refundRequest.Refund;
        Assert.NotNull(refund);
        Assert.Equal(0.0005m, refund.Amount);
        Assert.Equal("CN-0001", refund.Number);
        Assert.Equal(30.00m, refund.FiatAmount);
        Assert.Equal("https://velirapay.com/c/k3v9x2m7q8wz/credit-notes/CN-0001", refund.CreditNoteUrl);
        Assert.Equal(Utc("2026-09-21T14:29:41Z"), refund.ConfirmedOnChainAt);
    }

    [Fact]
    public void OnlyARefundThatStillSetsMoneyAsideIsOpen()
    {
        Assert.Equal(
            [true, true, true, false, false, false, false],
            new[] { "awaiting_address", "awaiting_transfer", "confirming", "refunded", "canceled", "expired", "refunding" }
                .Select(status => new RefundRequest(Element(Fixture("refund_request").With(("status", status)))).IsOpen));
    }

    [Fact]
    public async Task ARefundsAddressIsGivenAndItsTransferMarkedSent()
    {
        var http = new MockHttp()
            .Json(Data(Fixture("refund_request").With(("status", "awaiting_transfer"), ("address_given_by", "merchant"))))
            .Json(Data(Fixture("refund_request")));
        var client = Client(http);

        var withAddress = await client.Charges.SetRefundAddressAsync("k3v9x2m7q8wz", "r7d4f1k9m2qa", new RefundAddressOptions { Address = "bc1qar0srrr7xfkvy5l643lydnw9re59gtzzwf5mdq" }, new RequestOptions { IdempotencyKey = "address-1" });

        Assert.Equal(HttpMethod.Post, http.LastRequest.Method);
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/refund-requests/r7d4f1k9m2qa/address", http.LastRequest.Path);
        Assert.Equal("address-1", http.LastRequest.Header("Idempotency-Key"));
        Assert.Equal("""{"address":"bc1qar0srrr7xfkvy5l643lydnw9re59gtzzwf5mdq"}""", http.LastRequest.Body);
        Assert.Equal("merchant", withAddress.AddressGivenBy);

        var sent = await client.Charges.MarkRefundSentAsync("k3v9x2m7q8wz", "r7d4f1k9m2qa", new RefundSentOptions { Txid = "c1f0e2d3b4a5968778695a4b3c2d1e0f9e8d7c6b5a49382716f5e4d3c2b1a090" }, new RequestOptions { IdempotencyKey = "sent-1" });

        Assert.Equal(HttpMethod.Post, http.LastRequest.Method);
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/refund-requests/r7d4f1k9m2qa/sent", http.LastRequest.Path);
        Assert.Equal("sent-1", http.LastRequest.Header("Idempotency-Key"));
        Assert.Equal("""{"txid":"c1f0e2d3b4a5968778695a4b3c2d1e0f9e8d7c6b5a49382716f5e4d3c2b1a090"}""", http.LastRequest.Body);
        Assert.Equal(RefundRequestStatus.Confirming, sent.Status);
    }

    [Fact]
    public async Task ARefundIsCompletedOnYourWordWithOrWithoutAHash()
    {
        var refunded = Data(Fixture("refund_request").With(("status", "refunded"), ("refund", Fixture("charge")["refunds"]![0]!.AsObject().With(("confirmed_on_chain_at", null)))));
        var http = new MockHttp().Json(refunded).Json(refunded).Json(refunded);
        var client = Client(http);

        var completed = await client.Charges.CompleteRefundAsync("k3v9x2m7q8wz", "r7d4f1k9m2qa", requestOptions: new RequestOptions { IdempotencyKey = "complete-1" });

        Assert.Equal(HttpMethod.Post, http.LastRequest.Method);
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/refund-requests/r7d4f1k9m2qa/complete", http.LastRequest.Path);
        Assert.Equal("complete-1", http.LastRequest.Header("Idempotency-Key"));
        Assert.Null(http.LastRequest.Body);
        Assert.Null(http.LastRequest.Header("Content-Type"));
        Assert.True(completed.IsRefunded);
        Assert.Null(completed.Refund?.ConfirmedOnChainAt);

        await client.Charges.CompleteRefundAsync("k3v9x2m7q8wz", "r7d4f1k9m2qa", new RefundCompleteOptions());

        Assert.Null(http.LastRequest.Body);

        await client.Charges.CompleteRefundAsync("k3v9x2m7q8wz", "r7d4f1k9m2qa", new RefundCompleteOptions { Txid = "c1f0e2d3" });

        Assert.Equal("""{"txid":"c1f0e2d3"}""", http.LastRequest.Body);
    }

    [Fact]
    public async Task ARefundsLinkIsSentAgainAndARefundIsCanceled()
    {
        var http = new MockHttp()
            .Json(Data(AwaitingAddress().With(("emailed_at", "2026-09-22T09:00:00.000000Z"))))
            .Json(Data(AwaitingAddress().With(("status", "canceled"), ("address_url", null), ("canceled_at", "2026-09-22T09:05:00.000000Z"))));
        var client = Client(http);

        var resent = await client.Charges.ResendRefundLinkAsync("k3v9x2m7q8wz", "r7d4f1k9m2qa", new RequestOptions { IdempotencyKey = "resend-1" });

        Assert.Equal(HttpMethod.Post, http.LastRequest.Method);
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/refund-requests/r7d4f1k9m2qa/resend", http.LastRequest.Path);
        Assert.Equal("resend-1", http.LastRequest.Header("Idempotency-Key"));
        Assert.Null(http.LastRequest.Body);
        Assert.Null(http.LastRequest.Header("Content-Type"));
        Assert.Equal(Utc("2026-09-22T09:00:00Z"), resent.EmailedAt);

        var canceled = await client.Charges.CancelRefundAsync("k3v9x2m7q8wz", "r7d4f1k9m2qa", new RequestOptions { IdempotencyKey = "cancel-refund-1" });

        Assert.Equal(HttpMethod.Post, http.LastRequest.Method);
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/refund-requests/r7d4f1k9m2qa/cancel", http.LastRequest.Path);
        Assert.Equal("cancel-refund-1", http.LastRequest.Header("Idempotency-Key"));
        Assert.Null(http.LastRequest.Body);
        Assert.Equal(RefundRequestStatus.Canceled, canceled.Status);
        Assert.False(canceled.IsOpen);
        Assert.Equal(Utc("2026-09-22T09:05:00Z"), canceled.CanceledAt);
    }

    [Fact]
    public async Task AChargesRefundRequestsAreListedAndWalkedPageByPage()
    {
        var http = new MockHttp()
            .Json(ListResponse([Fixture("refund_request")], currentPage: 1, lastPage: 2, perPage: 1, total: 2))
            .Json(ListResponse([AwaitingAddress().With(("id", "q2w8e4r6t0yu"))], currentPage: 2, lastPage: 2, perPage: 1, total: 2));

        var page = await Client(http).Charges.RefundRequestsAsync("k3v9x2m7q8wz", new ListOptions { PerPage = 1 });

        Assert.Equal(HttpMethod.Get, http.LastRequest.Method);
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/refund-requests", http.LastRequest.Path);
        Assert.Equal("per_page=1", http.LastRequest.Query);
        Assert.IsType<RefundRequest>(Assert.Single(page));
        Assert.True(page.HasMore);

        var all = new List<RefundRequest>();

        await foreach (var refundRequest in page.AutoPagingIteratorAsync())
        {
            all.Add(refundRequest);
        }

        Assert.Equal(["r7d4f1k9m2qa", "q2w8e4r6t0yu"], all.Select(refundRequest => refundRequest.Id));
        Assert.Equal("/v1/charges/k3v9x2m7q8wz/refund-requests", http.LastRequest.Path);
        Assert.Equal(new Dictionary<string, string> { ["per_page"] = "1", ["page"] = "2" }, http.LastRequest.QueryParameters);
    }

    [Fact]
    public async Task BothIdsAreEscapedIntoThePathAndAnEmptyOneIsRefused()
    {
        var http = new MockHttp().Json(Data(Fixture("refund_request")));
        var client = Client(http);

        await client.Charges.RetrieveRefundRequestAsync("k3v 9x", "../r7d4");

        Assert.Equal("/v1/charges/k3v%209x/refund-requests/..%2Fr7d4", http.LastRequest.Path);

        var exception = await Assert.ThrowsAsync<InvalidArgumentException>(() => client.Charges.CancelRefundAsync("k3v9x2m7q8wz", " "));

        Assert.Equal("The id in /v1/charges/{id}/refund-requests/{id}/cancel cannot be empty.", exception.Message);
        await Assert.ThrowsAsync<InvalidArgumentException>(() => client.Charges.RefundRequestsAsync(string.Empty));
        await Assert.ThrowsAsync<InvalidArgumentException>(() => client.Charges.MarkRefundSentAsync("k3v9x2m7q8wz", "..", new RefundSentOptions { Txid = "c1f0e2d3" }));
        Assert.Single(http.Requests);
    }

    /// <summary>A refund just started, waiting for the customer to give the address.</summary>
    private static JsonObject AwaitingAddress() => Fixture("refund_request").With(
        ("status", "awaiting_address"),
        ("address", null),
        ("address_given_by", null),
        ("address_url", AddressUrl),
        ("txid", null),
        ("detected_txid", null),
        ("detected_amount", null),
        ("confirmations", null));
}
