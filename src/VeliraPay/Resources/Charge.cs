using System.Text.Json;

namespace VeliraPay;

/// <summary>A single payment in one coin, with the rate locked and a deposit address of its own.</summary>
public sealed class Charge : ApiResource
{
    /// <summary>Read a charge from the JSON the API sent.</summary>
    /// <param name="json">The charge, as a JSON object.</param>
    public Charge(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Id = JsonValues.String(data, "id", JsonValues.String(data, "code"));
        Status = JsonValues.String(data, "status");
        PaymentLink = JsonValues.Reference(data, "payment_link");
        PaymentLinkTitle = JsonValues.TryGet(data, "payment_link", out var paymentLink) ? JsonValues.NullableString(paymentLink, "title") : null;
        Invoice = JsonValues.Reference(data, "invoice");
        InvoiceNumber = JsonValues.TryGet(data, "invoice", out var invoice) ? JsonValues.NullableString(invoice, "number") : null;
        Description = JsonValues.NullableString(data, "description");
        FiatAmount = JsonValues.Decimal(data, "fiat_amount");
        FiatCurrency = JsonValues.String(data, "fiat_currency");
        Asset = JsonValues.String(data, "asset");
        AssetAmount = JsonValues.Decimal(data, "asset_amount");
        NetworkSurcharge = JsonValues.NullableDecimal(data, "network_surcharge");
        ProcessingTerms = JsonValues.NullableObject(data, "processing_terms", terms => new ProcessingTerms(terms));
        SwitchedFrom = JsonValues.NullableObject(data, "switched_from", quote => new OriginalQuote(quote));
        ReceivedAmount = JsonValues.NullableDecimal(data, "received_amount");
        RemainingAmount = JsonValues.Decimal(data, "remaining_amount");
        Overpaid = JsonValues.Bool(data, "overpaid");
        RefundedAmount = JsonValues.Decimal(data, "refunded_amount");
        Refunds = JsonValues.Objects(data, "refunds", refund => new Refund(refund));
        ExchangeRate = JsonValues.Decimal(data, "exchange_rate");
        DepositAddress = JsonValues.String(data, "deposit_address");
        PaymentRequest = JsonValues.NullableString(data, "payment_request");
        Transactions = JsonValues.Objects(data, "transactions", transaction => new Transaction(transaction));
        Timeline = [.. JsonValues.Records(data, "timeline").Select(entry => new ChargeTimelineEntry(JsonValues.String(entry, "event"), JsonValues.Date(entry, "time")))];
        CustomerEmail = JsonValues.NullableString(data, "customer_email");
        CustomerName = JsonValues.NullableString(data, "customer_name");
        Customer = new Customer(MergedCustomer(data, CustomerEmail, CustomerName));
        CustomFields = [.. JsonValues.Records(data, "custom_fields").Select(field => new CustomFieldAnswer(JsonValues.String(field, "label"), JsonValues.String(field, "value")))];
        Metadata = JsonValues.Map(data, "metadata");
        CheckoutUrl = JsonValues.NullableString(data, "checkout_url");
        ReceiptUrl = JsonValues.NullableString(data, "receipt_url");
        ExpiresAt = JsonValues.Date(data, "expires_at");
        PaidAt = JsonValues.Date(data, "paid_at");
        CreatedAt = JsonValues.Date(data, "created_at");
        Coupon = JsonValues.NullableString(data, "coupon");
        DiscountAmount = JsonValues.NullableDecimal(data, "discount_amount");
        SuccessUrl = JsonValues.NullableString(data, "success_url");
        CancelUrl = JsonValues.NullableString(data, "cancel_url");
        SuccessMessage = JsonValues.NullableString(data, "success_message");
    }

    /// <summary>The charge's public code, also the last segment of its checkout URL.</summary>
    public string Id { get; }

    /// <summary>One of the <see cref="ChargeStatus"/> values, or one the API added after this version of the library.</summary>
    public string Status { get; }

    /// <summary>The id of the payment link the charge was made through.</summary>
    public string? PaymentLink { get; }

    /// <summary>
    /// The title of the payment link the charge was made through, which webhooks carry beside its id;
    /// null in the API's own answers, which give the id alone, and for a charge made without a link.
    /// </summary>
    public string? PaymentLinkTitle { get; }

    /// <summary>The id of the invoice the charge pays.</summary>
    public string? Invoice { get; }

    /// <summary>
    /// The number of the invoice the charge pays, such as "INV-0042", which webhooks carry beside its id;
    /// null in the API's own answers, which give the id alone, and for a charge that pays no invoice.
    /// </summary>
    public string? InvoiceNumber { get; }

    /// <summary>The merchant's description of the charge.</summary>
    public string? Description { get; }

    /// <summary>What the customer owes, in fiat.</summary>
    public decimal FiatAmount { get; }

    /// <summary>The fiat currency, such as "USD".</summary>
    public string FiatCurrency { get; }

    /// <summary>
    /// The coin and network paid with, one of the <see cref="VeliraPay.Asset"/> values. It can change once, from TRX to USDT on Tron or back,
    /// when the customer paid the charge's address in the other coin: see <see cref="SwitchedFrom"/>.
    /// </summary>
    public string Asset { get; }

    /// <summary>The exact amount of the coin to send, network surcharge included, in the charge's current coin.</summary>
    public decimal AssetAmount { get; }

    /// <summary>What of <see cref="AssetAmount"/> the customer pays on top of the price for the network, which VeliraPay keeps; null for a coin without one.</summary>
    public decimal? NetworkSurcharge { get; }

    /// <summary>The processing fee and the most forwarding may cost, fixed when the charge was quoted; null for older charges and for VeliraPay's own billing.</summary>
    public ProcessingTerms? ProcessingTerms { get; }

    /// <summary>
    /// The quote the charge first had, when it was quoted again in the coin that arrived; null for almost every charge.
    /// TRX and USDT on Tron (USDT_TRON) share deposit addresses, so a charge that expired or was canceled with nothing of its own coin
    /// while the other arrived is quoted again in that one: <see cref="Asset"/>, <see cref="AssetAmount"/>, <see cref="ExchangeRate"/>
    /// and <see cref="NetworkSurcharge"/> then describe the new coin, <see cref="FiatAmount"/> stays the same, and the transfer waits as
    /// a late payment to accept. A charge is quoted again at most once.
    /// </summary>
    public OriginalQuote? SwitchedFrom { get; }

    /// <summary>What has been credited so far, or null before anything arrived.</summary>
    public decimal? ReceivedAmount { get; }

    /// <summary>What is still missing.</summary>
    public decimal RemainingAmount { get; }

    /// <summary>Whether the customer sent more than the overpayment threshold allows.</summary>
    public bool Overpaid { get; }

    /// <summary>How much has been recorded as refunded.</summary>
    public decimal RefundedAmount { get; }

    /// <summary>The refunds recorded against the charge; absent from webhooks.</summary>
    public IReadOnlyList<Refund> Refunds { get; }

    /// <summary>Fiat per one unit of the coin, locked when the charge was quoted; for a charge quoted again in another coin, that coin's, with the first in <see cref="SwitchedFrom"/>.</summary>
    public decimal ExchangeRate { get; }

    /// <summary>The address the customer pays into, or the payment hash of the Lightning invoice for <see cref="VeliraPay.Asset.BTC_LIGHTNING"/>.</summary>
    public string DepositAddress { get; }

    /// <summary>The Lightning invoice (BOLT11) the customer pays, for <see cref="VeliraPay.Asset.BTC_LIGHTNING"/>; null for every other coin.</summary>
    public string? PaymentRequest { get; }

    /// <summary>The transfers seen into the deposit address.</summary>
    public IReadOnlyList<Transaction> Transactions { get; }

    /// <summary>The charge's history, oldest first; absent from webhooks and from charges embedded in events.</summary>
    public IReadOnlyList<ChargeTimelineEntry> Timeline { get; }

    /// <summary>The customer's email address.</summary>
    public string? CustomerEmail { get; }

    /// <summary>The customer's name.</summary>
    public string? CustomerName { get; }

    /// <summary>Everything known about who pays, including the IP address and browser they paid from.</summary>
    public Customer Customer { get; }

    /// <summary>Labelled details: the customer's answers to the payment link's questions, or what was given when creating the charge.</summary>
    public IReadOnlyList<CustomFieldAnswer> CustomFields { get; }

    /// <summary>The merchant's own key-value data.</summary>
    public IReadOnlyDictionary<string, JsonElement> Metadata { get; }

    /// <summary>The hosted checkout page to send the customer to; absent from webhooks, so null there.</summary>
    public string? CheckoutUrl { get; }

    /// <summary>The customer's receipt, once the charge is paid; absent from webhooks.</summary>
    public string? ReceiptUrl { get; }

    /// <summary>When the locked rate runs out.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>When the charge was paid.</summary>
    public DateTimeOffset? PaidAt { get; }

    /// <summary>When the charge was created.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>The code of the coupon the customer used.</summary>
    public string? Coupon { get; }

    /// <summary>What the coupon took off, in fiat; <see cref="FiatAmount"/> is already the lower price.</summary>
    public decimal? DiscountAmount { get; }

    /// <summary>The page the customer is offered a button back to once they have paid; absent from webhooks.</summary>
    public string? SuccessUrl { get; }

    /// <summary>The page the customer can go back to from the checkout without paying; absent from webhooks.</summary>
    public string? CancelUrl { get; }

    /// <summary>What the customer is told once they have paid; absent from webhooks.</summary>
    public string? SuccessMessage { get; }

    /// <summary>Whether the charge is waiting for its payment.</summary>
    public bool IsPending => Status == ChargeStatus.Pending;

    /// <summary>Whether less than the amount due has arrived.</summary>
    public bool IsUnderpaid => Status == ChargeStatus.Underpaid;

    /// <summary>Whether the charge is paid in full and confirmed.</summary>
    public bool IsPaid => Status == ChargeStatus.Paid;

    /// <summary>Whether the locked rate ran out before the charge was paid.</summary>
    public bool IsExpired => Status == ChargeStatus.Expired;

    /// <summary>Whether the charge was canceled.</summary>
    public bool IsCanceled => Status == ChargeStatus.Canceled;

    /// <summary>Whether the charge can still be paid.</summary>
    public bool IsOpen => IsPending || IsUnderpaid;

    /// <summary>The customer object, with the older customer_email and customer_name standing in for what it does not say.</summary>
    private static JsonElement MergedCustomer(JsonElement data, string? email, string? name)
    {
        var customer = JsonValues.EnumerateMap(data, "customer").ToList();

        return JsonValues.Write(writer =>
        {
            writer.WriteStartObject();

            foreach (var (key, fallback) in new[] { ("email", email), ("name", name) })
            {
                writer.WritePropertyName(key);

                if (customer.FindLast(entry => entry.Key == key) is { Key: not null } given)
                {
                    given.Value.WriteTo(writer);
                }
                else if (fallback is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteStringValue(fallback);
                }
            }

            foreach (var (key, value) in customer.Where(entry => entry.Key is not ("email" or "name")))
            {
                writer.WritePropertyName(key);
                value.WriteTo(writer);
            }

            writer.WriteEndObject();
        });
    }
}

/// <summary>One entry of a charge's history.</summary>
/// <param name="Event">What happened, one of the <see cref="EventType"/> values.</param>
/// <param name="Time">When it happened.</param>
public sealed record ChargeTimelineEntry(string Event, DateTimeOffset? Time);

/// <summary>A labelled detail of a charge: an answer to a payment link's question, or one given when creating the charge.</summary>
/// <param name="Label">What the detail is, such as "Company".</param>
/// <param name="Value">The detail itself.</param>
public sealed record CustomFieldAnswer(string Label, string Value);

/// <summary>What is known about the customer paying a charge, including the signals that help spot fraud.</summary>
public sealed class Customer : ApiResource
{
    /// <summary>Read a customer from the JSON the API sent.</summary>
    /// <param name="json">The customer, as a JSON object.</param>
    public Customer(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Email = JsonValues.NullableString(data, "email");
        Name = JsonValues.NullableString(data, "name");
        IpAddress = JsonValues.NullableString(data, "ip_address");
        IpCountry = JsonValues.NullableString(data, "ip_country");
        IpAsn = JsonValues.NullableInt(data, "ip_asn");
        IpNetwork = JsonValues.NullableString(data, "ip_network");
        UserAgent = JsonValues.NullableString(data, "user_agent");
        Reference = JsonValues.NullableString(data, "reference");
        Phone = JsonValues.NullableString(data, "phone");
        Country = JsonValues.NullableString(data, "country");
        Metadata = JsonValues.Map(data, "metadata");
    }

    /// <summary>The customer's email address.</summary>
    public string? Email { get; }

    /// <summary>The customer's name.</summary>
    public string? Name { get; }

    /// <summary>The IP address the customer paid from, recorded by the checkout or given when the charge was created.</summary>
    public string? IpAddress { get; }

    /// <summary>The country the hosted checkout placed the IP address in, as an ISO 3166 two-letter code, or T1 for Tor; null for a charge created through the API.</summary>
    public string? IpCountry { get; }

    /// <summary>The number of the network (autonomous system) the IP address belongs to, such as 24940.</summary>
    public int? IpAsn { get; }

    /// <summary>Who runs that network, such as an internet provider or a hosting company.</summary>
    public string? IpNetwork { get; }

    /// <summary>The user agent of the browser the customer paid from.</summary>
    public string? UserAgent { get; }

    /// <summary>Your own id for the customer.</summary>
    public string? Reference { get; }

    /// <summary>The customer's phone number.</summary>
    public string? Phone { get; }

    /// <summary>The customer's country, as an ISO 3166 two-letter code.</summary>
    public string? Country { get; }

    /// <summary>Anything else you gave about the customer.</summary>
    public IReadOnlyDictionary<string, JsonElement> Metadata { get; }
}

/// <summary>The processing fee and forwarding limit fixed when a charge was quoted.</summary>
public sealed class ProcessingTerms : ApiResource
{
    /// <summary>Read processing terms from the JSON the API sent.</summary>
    /// <param name="json">The terms, as a JSON object.</param>
    public ProcessingTerms(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Percent = JsonValues.Decimal(data, "percent");
        MaxForwardingPercent = JsonValues.Int(data, "max_forwarding_percent");
    }

    /// <summary>The processing fee, in percent of what the charge took in, network surcharge left out, such as 1.75.</summary>
    public decimal Percent { get; }

    /// <summary>The most passing a managed payment on to the payout address may cost, in percent of the merchant's share after the fee, such as 10.</summary>
    public int MaxForwardingPercent { get; }
}

/// <summary>The quote a charge first had, before it was quoted again in the coin that arrived at its address.</summary>
public sealed class OriginalQuote : ApiResource
{
    /// <summary>Read a charge's first quote from the JSON the API sent.</summary>
    /// <param name="json">The quote, as a JSON object.</param>
    public OriginalQuote(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Asset = JsonValues.String(data, "asset");
        AssetAmount = JsonValues.Decimal(data, "asset_amount");
        ExchangeRate = JsonValues.Decimal(data, "exchange_rate");
        SwitchedAt = JsonValues.Date(data, "switched_at");
    }

    /// <summary>The coin the charge was first quoted in, one of the <see cref="VeliraPay.Asset"/> values.</summary>
    public string Asset { get; }

    /// <summary>The amount of that coin it first asked for, network surcharge included.</summary>
    public decimal AssetAmount { get; }

    /// <summary>Fiat per one unit of that coin, as first locked.</summary>
    public decimal ExchangeRate { get; }

    /// <summary>When the charge was quoted again in the coin that arrived.</summary>
    public DateTimeOffset? SwitchedAt { get; }
}

/// <summary>A blockchain transfer into a charge's deposit address.</summary>
public sealed class Transaction : ApiResource
{
    /// <summary>Read a transaction from the JSON the API sent.</summary>
    /// <param name="json">The transaction, as a JSON object.</param>
    public Transaction(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Txid = JsonValues.String(data, "txid");
        Amount = JsonValues.Decimal(data, "amount");
        Confirmations = JsonValues.Int(data, "confirmations");
        RequiredConfirmations = JsonValues.Int(data, "required_confirmations");
        Credited = JsonValues.Bool(data, "credited");
        ExplorerUrl = JsonValues.NullableString(data, "explorer_url");
        SeenAt = JsonValues.Date(data, "seen_at");
    }

    /// <summary>The transaction id on its chain.</summary>
    public string Txid { get; }

    /// <summary>The amount received, in the charge's coin.</summary>
    public decimal Amount { get; }

    /// <summary>How many confirmations the transaction has.</summary>
    public int Confirmations { get; }

    /// <summary>How many confirmations the coin needs before the transfer counts.</summary>
    public int RequiredConfirmations { get; }

    /// <summary>Whether the transfer has been counted towards the charge.</summary>
    public bool Credited { get; }

    /// <summary>A link to the transaction on a block explorer.</summary>
    public string? ExplorerUrl { get; }

    /// <summary>When the transfer was first seen.</summary>
    public DateTimeOffset? SeenAt { get; }
}

/// <summary>A refund recorded against a charge. The transfer itself is sent by the merchant.</summary>
public sealed class Refund : ApiResource
{
    /// <summary>Read a refund from the JSON the API sent.</summary>
    /// <param name="json">The refund, as a JSON object.</param>
    public Refund(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Amount = JsonValues.Decimal(data, "amount");
        Txid = JsonValues.NullableString(data, "txid");
        Reason = JsonValues.NullableString(data, "reason");
        ExplorerUrl = JsonValues.NullableString(data, "explorer_url");
        CreatedAt = JsonValues.Date(data, "created_at");
    }

    /// <summary>The amount sent back, in the charge's coin.</summary>
    public decimal Amount { get; }

    /// <summary>The transaction that carried the refund, when it was recorded.</summary>
    public string? Txid { get; }

    /// <summary>Why the refund was made.</summary>
    public string? Reason { get; }

    /// <summary>A link to the refund transaction on a block explorer.</summary>
    public string? ExplorerUrl { get; }

    /// <summary>When the refund was recorded.</summary>
    public DateTimeOffset? CreatedAt { get; }
}
