using System.Text.Json;

namespace VeliraPay;

/// <summary>What became of one paid charge: what came in, what was kept, what was sent and the transaction that carried it.</summary>
public sealed class Payout : ApiResource
{
    /// <summary>Read a payout from the JSON the API sent.</summary>
    /// <param name="json">The payout, as a JSON object.</param>
    public Payout(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Id = JsonValues.String(data, "id");
        Charge = JsonValues.String(data, "charge");
        Status = JsonValues.String(data, "status");
        Asset = JsonValues.String(data, "asset");
        GrossAmount = JsonValues.Decimal(data, "gross_amount");
        FeePercent = JsonValues.Decimal(data, "fee_percent");
        FeeAmount = JsonValues.Decimal(data, "fee_amount");
        FeeFiatAmount = JsonValues.NullableDecimal(data, "fee_fiat_amount");
        FiatCurrency = JsonValues.NullableString(data, "fiat_currency");
        ExchangeRate = JsonValues.NullableDecimal(data, "exchange_rate");
        NetAmount = JsonValues.Decimal(data, "net_amount");
        NetworkFee = JsonValues.NullableDecimal(data, "network_fee");
        SentAmount = JsonValues.NullableDecimal(data, "sent_amount");
        PayoutAddress = JsonValues.NullableString(data, "payout_address");
        PayoutTxid = JsonValues.NullableString(data, "payout_txid");
        ExplorerUrl = JsonValues.NullableString(data, "explorer_url");
        Attempts = JsonValues.Int(data, "attempts");
        LastError = JsonValues.NullableString(data, "last_error");
        NextAttemptAt = JsonValues.Date(data, "next_attempt_at");
        SentAt = JsonValues.Date(data, "sent_at");
        ConfirmedAt = JsonValues.Date(data, "confirmed_at");
        CreatedAt = JsonValues.Date(data, "created_at");
    }

    /// <summary>The id of the charge the payout came from, which is also its own.</summary>
    public string Id { get; }

    /// <summary>The id of the charge the payout came from.</summary>
    public string Charge { get; }

    /// <summary>One of the <see cref="PayoutStatus"/> values: pending, sending, sent, failed or manual.</summary>
    public string Status { get; }

    /// <summary>The coin it was paid in and goes out in.</summary>
    public string Asset { get; }

    /// <summary>What the customer paid, in the coin.</summary>
    public decimal GrossAmount { get; }

    /// <summary>The rate of the platform's fee, in percent.</summary>
    public decimal FeePercent { get; }

    /// <summary>What was kept, in the coin.</summary>
    public decimal FeeAmount { get; }

    /// <summary>What the fee was worth in fiat, at the rate the charge was locked at.</summary>
    public decimal? FeeFiatAmount { get; }

    /// <summary>The fiat currency of <see cref="FeeFiatAmount"/> and <see cref="ExchangeRate"/>.</summary>
    public string? FiatCurrency { get; }

    /// <summary>The rate the charge was locked at.</summary>
    public decimal? ExchangeRate { get; }

    /// <summary>What the merchant is owed, before the chain's own fee.</summary>
    public decimal NetAmount { get; }

    /// <summary>What the chain charged, once it has been broadcast.</summary>
    public decimal? NetworkFee { get; }

    /// <summary>What reached the merchant, once it has been broadcast.</summary>
    public decimal? SentAmount { get; }

    /// <summary>Where it goes, or null while it waits for an address.</summary>
    public string? PayoutAddress { get; }

    /// <summary>The transaction that carried it.</summary>
    public string? PayoutTxid { get; }

    /// <summary>The transaction's page on a block explorer.</summary>
    public string? ExplorerUrl { get; }

    /// <summary>How many times forwarding has been tried.</summary>
    public int Attempts { get; }

    /// <summary>Why the last try did not work, or why it is held.</summary>
    public string? LastError { get; }

    /// <summary>When it may go out next.</summary>
    public DateTimeOffset? NextAttemptAt { get; }

    /// <summary>When it was broadcast.</summary>
    public DateTimeOffset? SentAt { get; }

    /// <summary>When it confirmed.</summary>
    public DateTimeOffset? ConfirmedAt { get; }

    /// <summary>When the books were written.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>Whether the payout has confirmed, so the money is in the merchant's wallet.</summary>
    public bool IsSent => Status == PayoutStatus.Sent;

    /// <summary>Whether the payout waits on an address or on a person.</summary>
    public bool IsManual => Status == PayoutStatus.Manual;
}

/// <summary>What one coin adds up to across the account's payouts.</summary>
public sealed class PayoutSummary : ApiResource
{
    /// <summary>Read a summary row from the JSON the API sent.</summary>
    /// <param name="json">The row, as a JSON object.</param>
    public PayoutSummary(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Asset = JsonValues.String(data, "asset");
        Payouts = JsonValues.Int(data, "payouts");
        GrossAmount = JsonValues.Decimal(data, "gross_amount");
        FeeAmount = JsonValues.Decimal(data, "fee_amount");
        PaidOutAmount = JsonValues.Decimal(data, "paid_out_amount");
        InFlightAmount = JsonValues.Decimal(data, "in_flight_amount");
        OwedAmount = JsonValues.Decimal(data, "owed_amount");
        NetworkFeeAmount = JsonValues.Decimal(data, "network_fee_amount");
        WaitingForAddress = JsonValues.Int(data, "waiting_for_address");
    }

    /// <summary>The coin this row adds up.</summary>
    public string Asset { get; }

    /// <summary>How many paid charges it covers.</summary>
    public int Payouts { get; }

    /// <summary>What came in.</summary>
    public decimal GrossAmount { get; }

    /// <summary>What was kept in fees.</summary>
    public decimal FeeAmount { get; }

    /// <summary>What has reached the merchant.</summary>
    public decimal PaidOutAmount { get; }

    /// <summary>What is on its way and waiting to confirm.</summary>
    public decimal InFlightAmount { get; }

    /// <summary>What is still owed and has not left yet.</summary>
    public decimal OwedAmount { get; }

    /// <summary>What the chains charged to forward it.</summary>
    public decimal NetworkFeeAmount { get; }

    /// <summary>How many payouts wait for an address for this coin.</summary>
    public int WaitingForAddress { get; }
}
