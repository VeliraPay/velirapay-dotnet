using System.Globalization;
using System.Text;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

public sealed class WebhookTests
{
    private const string Secret = "whsec_2b7Qf9LkXw3NcR8vT1mZ5pYs0HdJ4gUe6AoKiVbE";

    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeSeconds(1790000000);

    [Fact]
    public void ASignedChargeDeliveryIsVerifiedAndParsed()
    {
        var webhookEvent = Signed("webhook_charge");

        Assert.Equal("5f0c6f5e-8d1b-4a53-9a57-3c2b1d0e9f87", webhookEvent.Id);
        Assert.Equal("9b1f7a3e-2c4d-4e8f-a6b0-1d2c3e4f5a6b", webhookEvent.EventId);
        Assert.Equal("charge.paid", webhookEvent.Type);
        Assert.True(webhookEvent.Is(EventType.ChargePaid));
        Assert.True(webhookEvent.IsLiveMode);
        Assert.False(webhookEvent.Test);
        Assert.Equal(Utc("2026-09-20T10:05:42Z"), webhookEvent.CreatedAt);
        Assert.Null(webhookEvent.Invoice);
        Assert.Null(webhookEvent.Transaction);

        var charge = webhookEvent.Charge;
        Assert.NotNull(charge);
        Assert.Equal("k3v9x2m7q8wz", charge.Id);
        Assert.True(charge.IsPaid);
        Assert.Equal("p4n8r2t6y1ua", charge.PaymentLink);
        Assert.Equal("Pro plan", charge.PaymentLinkTitle);
        Assert.Null(charge.Invoice);
        Assert.Null(charge.InvoiceNumber);
        Assert.Equal("0.002500000000000000", charge.AssetAmount.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(2, charge.Transactions[0].Confirmations);
        Assert.Equal(2, charge.Transactions[0].RequiredConfirmations);
        Assert.Equal("203.0.113.7", charge.Customer.IpAddress);
        Assert.Equal("ada@example.com", charge.Customer.Email);
        Assert.Empty(charge.Customer.Metadata);
        Assert.Equal("1042", charge.Metadata["order_id"].GetString());
        Assert.Empty(charge.Refunds);
        Assert.Null(charge.CheckoutUrl);
    }

    [Fact]
    public void AChargeThatPaysAnInvoiceCarriesItsNumber()
    {
        var delivery = Fixture("webhook_charge");
        delivery["data"]!["charge"] = delivery["data"]!["charge"]!.AsObject().With(("payment_link", null), ("invoice", Json("""{"code":"w7c2h5j9d3kx","number":"INV-0042"}""")));
        var payload = delivery.ToJsonString();

        var charge = Webhook.ConstructEvent(payload, Webhook.GenerateSignatureHeader(payload, Secret), Secret).Charge;

        Assert.NotNull(charge);
        Assert.Equal("w7c2h5j9d3kx", charge.Invoice);
        Assert.Equal("INV-0042", charge.InvoiceNumber);
        Assert.Null(charge.PaymentLink);
        Assert.Null(charge.PaymentLinkTitle);
    }

    [Fact]
    public void APaymentDetectedDeliveryCarriesTheTransferThatWasSeen()
    {
        var webhookEvent = Signed("webhook_payment_detected");

        Assert.True(webhookEvent.Is(EventType.ChargePaymentDetected));
        Assert.True(webhookEvent.Charge?.IsPending);

        var transaction = webhookEvent.Transaction;
        Assert.NotNull(transaction);
        Assert.Equal("7d1c5e9a3b2f8e6d4c0a9b8e7f6d5c4b3a2918f7e6d5c4b3a29180f7e6d5c4b3", transaction.Txid);
        Assert.Equal("0.002500000000000000", transaction.Amount.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(0, transaction.Confirmations);
        Assert.Equal(2, transaction.RequiredConfirmations);
        Assert.False(transaction.Credited);
        Assert.StartsWith("https://mempool.space/tx/", transaction.ExplorerUrl, StringComparison.Ordinal);
        Assert.Equal(Utc("2026-09-20T09:58:12Z"), transaction.SeenAt);
    }

    [Fact]
    public void APayoutDeliveryCarriesTheChargeAndItsPayout()
    {
        var webhookEvent = Signed("webhook_payout");

        Assert.True(webhookEvent.Is(EventType.PayoutConfirmed));
        Assert.Equal("k3v9x2m7q8wz", webhookEvent.Charge?.Id);
        Assert.Null(webhookEvent.Transaction);

        var payout = webhookEvent.Payout;
        Assert.NotNull(payout);
        Assert.Equal("k3v9x2m7q8wz", payout.Charge);
        Assert.True(payout.IsSent);
        Assert.Equal(0.002425m, payout.SentAmount);
        Assert.Equal(0.0000044m, payout.NetworkFee);
        Assert.Equal("cc9a3e0b5629235194a6174d805c55df5554c94e2b4de8dc584db2091cc5cacf", payout.PayoutTxid);
        Assert.Equal(Utc("2026-09-20T10:41:02Z"), payout.ConfirmedAt);
    }

    [Fact]
    public void OnlyAPayoutDeliveryCarriesAPayout()
    {
        Assert.Null(Signed("webhook_charge").Payout);
    }

    [Fact]
    public void ASignedInvoiceDeliveryIsParsed()
    {
        var webhookEvent = Signed("webhook_invoice");

        Assert.Equal("invoice.paid", webhookEvent.Type);
        Assert.False(webhookEvent.IsLiveMode);
        Assert.Null(webhookEvent.Charge);

        var invoice = webhookEvent.Invoice;
        Assert.NotNull(invoice);
        Assert.Equal("w7c2h5j9d3kx", invoice.Id);
        Assert.True(invoice.IsPaid);
        Assert.Null(invoice.Overdue);
        Assert.Null(invoice.DocumentUrl);
        Assert.Equal(["m2q7w4e9r1ty", "z8x6c4v2b0nm"], invoice.Charges.Select(charge => charge.Id));
        Assert.Equal(new DateOnly(2026, 10, 15), invoice.DueAt);
    }

    [Fact]
    public void SignaturesMadeByVeliraPayAreAccepted()
    {
        const string payload = """{"id":"5f0c6f5e-8d1b-4a53-9a57-3c2b1d0e9f87","event_id":"9b1f7a3e-2c4d-4e8f-a6b0-1d2c3e4f5a6b","event":"charge.paid","mode":"live","created_at":"2026-09-20T10:05:42.000000Z","data":{"charge":{"code":"k3v9x2m7q8wz","status":"paid"}}}""";
        const string header = "t=1790000000,v1=a7a0dd3b10e4302cf840472806bb1c3a75453b7f8a6975c6475f92d58826df35";

        Webhook.VerifySignature(payload, header, Secret, now: At.AddSeconds(60));
        Webhook.VerifySignature(Encoding.UTF8.GetBytes(payload), header, Secret, now: At.AddSeconds(60));

        Assert.Equal(header, Webhook.GenerateSignatureHeader(payload, Secret, At));
    }

    [Fact]
    public void TheBytesOfTheBodyAreSignedWhetherGivenAsAStringOrBytes()
    {
        var payload = Payload("webhook_charge").Replace("Pro plan, 1 year", "Zoë’s plan, 1 year", StringComparison.Ordinal);
        var header = Webhook.GenerateSignatureHeader(payload, Secret);
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(header[2..header.IndexOf(',', StringComparison.Ordinal)], CultureInfo.InvariantCulture));
        var bytes = Encoding.UTF8.GetBytes(payload);

        Assert.Equal(header, Webhook.GenerateSignatureHeader(bytes, Secret, timestamp));
        Assert.Equal("Zoë’s plan, 1 year", Webhook.ConstructEvent(bytes, header, Secret).Charge?.Description);
        Assert.Equal("5f0c6f5e-8d1b-4a53-9a57-3c2b1d0e9f87", Webhook.ConstructEvent(payload, header, Secret).Id);
    }

    [Fact]
    public void AChangedPayloadIsRejected()
    {
        var payload = Payload("webhook_charge");
        var header = Webhook.GenerateSignatureHeader(payload, Secret);

        Assert.Throws<SignatureVerificationException>(() => Webhook.ConstructEvent(payload.Replace("150.00", "1.00", StringComparison.Ordinal), header, Secret));
    }

    [Fact]
    public void ASignatureMadeWithAnotherSecretIsRejected()
    {
        var payload = Payload("webhook_charge");

        var exception = Assert.Throws<SignatureVerificationException>(() => Webhook.ConstructEvent(payload, Webhook.GenerateSignatureHeader(payload, "whsec_other"), Secret));

        Assert.Contains("does not match", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnOldSignatureIsRejectedUnlessTheToleranceIsOff()
    {
        var payload = Payload("webhook_charge");
        var header = Webhook.GenerateSignatureHeader(payload, Secret, DateTimeOffset.UtcNow.AddSeconds(-301));

        Webhook.VerifySignature(payload, header, Secret, TimeSpan.Zero);

        var exception = Assert.Throws<SignatureVerificationException>(() => Webhook.VerifySignature(payload, header, Secret));

        Assert.Contains("more than the 300 allowed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASignatureFromTheFutureIsHeldToTheSameTolerance()
    {
        var payload = Payload("webhook_charge");
        var header = Webhook.GenerateSignatureHeader(payload, Secret, At);

        Webhook.VerifySignature(payload, header, Secret, TimeSpan.FromSeconds(300), At.AddSeconds(-300));

        var exception = Assert.Throws<SignatureVerificationException>(() => Webhook.VerifySignature(payload, header, Secret, TimeSpan.FromSeconds(300), At.AddSeconds(-301)));

        Assert.Equal("The signature is 301 seconds away from now, more than the 300 allowed.", exception.Message);
    }

    [Fact]
    public void OneMatchingSignatureAmongSeveralIsEnough()
    {
        var payload = Payload("webhook_charge");
        var parts = Webhook.GenerateSignatureHeader(payload, Secret).Split(',');

        var webhookEvent = Webhook.ConstructEvent(payload, $"{parts[0]},v1={new string('0', 64)},{parts[1]}", Secret);

        Assert.Equal("charge.paid", webhookEvent.Type);
    }

    [Fact]
    public void DuringASecretRotationEitherSecretVerifiesTheDelivery()
    {
        var payload = Payload("webhook_charge");
        const string previousSecret = "whsec_previousXw3NcR8vT1mZ5pYs0HdJ4gUe6AoKiVbE";
        var current = Webhook.GenerateSignatureHeader(payload, Secret, At).Split(',');
        var previous = Webhook.GenerateSignatureHeader(payload, previousSecret, At).Split(',');
        var header = $"{current[0]},{current[1]},{previous[1]}";

        Webhook.VerifySignature(payload, header, Secret, now: At.AddSeconds(10));
        Webhook.VerifySignature(payload, header, previousSecret, now: At.AddSeconds(10));

        Assert.Throws<SignatureVerificationException>(() => Webhook.VerifySignature(payload, header, "whsec_neither", now: At.AddSeconds(10)));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("suffixed")]
    [InlineData("truncated")]
    [InlineData("upper case")]
    public void ASignatureOfAnotherLengthOrCaseIsAMismatchRatherThanACrash(string change)
    {
        var payload = Payload("webhook_charge");
        var parts = Webhook.GenerateSignatureHeader(payload, Secret).Split(',');
        var given = change switch
        {
            "short" => "v1=abc",
            "suffixed" => $"{parts[1]}00",
            "truncated" => parts[1][..^1],
            _ => $"v1={parts[1][3..].ToUpperInvariant()}",
        };

        var exception = Assert.Throws<SignatureVerificationException>(() => Webhook.ConstructEvent(payload, $"{parts[0]},{given}", Secret));

        Assert.Contains("does not match", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ANegativeSignatureToleranceIsAConfigurationError()
    {
        var payload = Payload("webhook_charge");
        var header = Webhook.GenerateSignatureHeader(payload, Secret, At);

        var exception = Assert.Throws<InvalidArgumentException>(() => Webhook.VerifySignature(payload, header, Secret, TimeSpan.FromSeconds(-1), At.AddSeconds(301)));

        Assert.Contains("tolerance cannot be negative", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, "header is missing")]
    [InlineData("", "header is missing")]
    [InlineData("  ", "header is missing")]
    [InlineData("garbage", "has no timestamp")]
    [InlineData("t=yesterday,v1=abc", "has no timestamp")]
    [InlineData("t=,v1=abc", "has no timestamp")]
    [InlineData("t=1790000000", "has no v1 signature")]
    [InlineData("t=1790000000,v0=abc", "has no v1 signature")]
    [InlineData("t=1790000000,v1=", "has no v1 signature")]
    public void AMalformedHeaderIsRejected(string? header, string message)
    {
        var exception = Assert.Throws<SignatureVerificationException>(() => Webhook.ConstructEvent(Payload("webhook_charge"), header, Secret));

        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AHeaderJoinedFromSeveralValuesIsRead()
    {
        var payload = Payload("webhook_charge");
        var parts = Webhook.GenerateSignatureHeader(payload, Secret).Split(',');

        Assert.Equal("charge.paid", Webhook.ConstructEvent(payload, $" {parts[0]} , {parts[1]} ", Secret).Type);
    }

    [Fact]
    public void ATimestampTooLargeForAnyClockIsTooOldRatherThanACrash()
    {
        var payload = Payload("webhook_charge");
        var signature = Webhook.GenerateSignatureHeader(payload, Secret).Split(',')[1];

        var exception = Assert.Throws<SignatureVerificationException>(() => Webhook.ConstructEvent(payload, $"t=99999999999999999999999999,{signature}", Secret));

        Assert.Contains("seconds away from now", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void AMissingSecretIsAConfigurationError(string? secret)
    {
        var payload = Payload("webhook_charge");

        Assert.Throws<InvalidArgumentException>(() => Webhook.ConstructEvent(payload, Webhook.GenerateSignatureHeader(payload, string.Empty), secret));
    }

    [Fact]
    public async Task ARequestBodyStreamIsReadVerifiedAndParsed()
    {
        var payload = Payload("webhook_charge");
        using var body = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        var webhookEvent = await Webhook.ConstructEventAsync(body, Webhook.GenerateSignatureHeader(payload, Secret), Secret);

        Assert.Equal("5f0c6f5e-8d1b-4a53-9a57-3c2b1d0e9f87", webhookEvent.Id);
        Assert.Equal("X-VeliraPay-Signature", Webhook.SignatureHeader);
        Assert.Equal("X-VeliraPay-Event", Webhook.EventHeader);
        Assert.Equal("X-VeliraPay-Delivery", Webhook.DeliveryHeader);
    }

    [Fact]
    public void TestDeliveriesFromTheDashboardAreFlagged()
    {
        const string payload = """{"id":"3c9e1f7a-5b2d-4e8c-9a1f-7b3d5e9c1a2f","event":"charge.paid","test":true,"mode":"test","created_at":"2026-09-24T09:00:00.000000Z","data":{"charge":{"code":"testab12cd34","status":"paid"}}}""";

        var webhookEvent = Webhook.ConstructEvent(payload, Webhook.GenerateSignatureHeader(payload, Secret), Secret);

        Assert.True(webhookEvent.Test);
        Assert.Null(webhookEvent.EventId);
        Assert.Equal("testab12cd34", webhookEvent.Charge?.Id);
    }

    [Fact]
    public void TheVerificationChallengeIsRecognisedAndAnswered()
    {
        const string payload = """{"id":"0199f0c2-2b7a-7c0e-9d3a-5e8f1a2b3c4d","event":"webhook.verification","mode":"live","created_at":"2026-10-19T22:00:00.000000Z","data":{"challenge":"c4Lr9xT2mQ7vZ1"}}""";

        var webhookEvent = Webhook.ConstructEvent(payload, Webhook.GenerateSignatureHeader(payload, Secret), Secret);

        Assert.True(webhookEvent.IsVerification);
        Assert.True(webhookEvent.Is(Webhook.VerificationEvent));
        Assert.Equal("c4Lr9xT2mQ7vZ1", webhookEvent.Challenge);
        Assert.Equal("""{"challenge":"c4Lr9xT2mQ7vZ1"}""", webhookEvent.ChallengeResponse());
    }

    [Fact]
    public void OtherDeliveriesHaveNoChallengeToAnswer()
    {
        var webhookEvent = Signed("webhook_charge");

        Assert.False(webhookEvent.IsVerification);
        Assert.Null(webhookEvent.Challenge);
        Assert.Null(webhookEvent.ChallengeResponse());
    }

    [Fact]
    public void ASignedPayloadThatIsNotAJsonObjectIsRejected()
    {
        const string list = """["charge.paid"]""";

        var exception = Assert.Throws<InvalidArgumentException>(() => Webhook.ConstructEvent(list, Webhook.GenerateSignatureHeader(list, Secret), Secret));

        Assert.Contains("not a JSON object", exception.Message, StringComparison.Ordinal);
        Assert.Contains("not valid JSON", Assert.Throws<InvalidArgumentException>(() => WebhookEvent.FromPayload(new byte[] { 0x7b, 0xff, 0x7d })).Message, StringComparison.Ordinal);
        Assert.Contains("not valid JSON", Assert.Throws<InvalidArgumentException>(() => WebhookEvent.FromPayload("{")).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEventKeepsWhatItWasSentWith()
    {
        var webhookEvent = Signed("webhook_charge");

        AssertJson(Payload("webhook_charge"), webhookEvent.ToJson());
        Assert.Equal("live", webhookEvent.Get("mode")?.GetString());
    }

    /// <summary>Construct the event of a fixture, signed now.</summary>
    private static WebhookEvent Signed(string name)
    {
        var payload = Payload(name);

        return Webhook.ConstructEvent(payload, Webhook.GenerateSignatureHeader(payload, Secret), Secret);
    }
}
