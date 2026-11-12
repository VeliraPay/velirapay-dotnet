using System.Text.Json;

namespace VeliraPay;

/// <summary>The dashboard's figures for the last 30 days in the API key's mode, with what needs attention now.</summary>
public sealed class Overview : ApiResource
{
    /// <summary>Read the overview from the JSON the API sent.</summary>
    /// <param name="json">The overview, as a JSON object.</param>
    public Overview(JsonElement json)
        : this(json, null)
    {
    }

    /// <summary>Read the overview from the JSON the API sent, with what its answer said besides.</summary>
    /// <param name="json">The overview, as a JSON object.</param>
    /// <param name="meta">What the answer said about it besides: the key's mode.</param>
    public Overview(JsonElement json, JsonElement? meta)
        : base(json)
    {
        var data = RawJson;
        var about = meta?.ValueKind == JsonValueKind.Object ? meta.Value.Clone() : JsonValues.EmptyObject;
        var period = JsonValues.Object(data, "period");
        var funnel = JsonValues.Object(data, "funnel");
        var attention = JsonValues.Object(data, "attention");
        var setup = JsonValues.Object(data, "setup");

        Currency = JsonValues.String(data, "currency");
        Volume = JsonValues.Decimal(data, "volume");
        VolumeChangePercent = JsonValues.NullableDecimal(data, "volume_change_percent");
        SentVolume = JsonValues.Decimal(data, "sent_volume");
        PaidCharges = JsonValues.Int(data, "paid_charges");
        ConversionRatePercent = JsonValues.NullableDecimal(data, "conversion_rate_percent");
        PendingCharges = JsonValues.Int(data, "pending_charges");
        ActivePaymentLinks = JsonValues.Int(data, "active_payment_links");
        OpenInvoices = JsonValues.Int(data, "open_invoices");
        OverdueInvoices = JsonValues.Int(data, "overdue_invoices");
        Period = new OverviewPeriod { From = JsonValues.Day(period, "from"), To = JsonValues.Day(period, "to") };
        Funnel = new OverviewFunnel
        {
            Started = JsonValues.Int(funnel, "started"),
            Paid = JsonValues.Int(funnel, "paid"),
            Waiting = JsonValues.Int(funnel, "waiting"),
            Abandoned = JsonValues.Int(funnel, "abandoned"),
            Incomplete = JsonValues.Int(funnel, "incomplete"),
            Assets = [.. JsonValues.Records(funnel, "assets").Select(row => new OverviewFunnelAsset
            {
                Asset = JsonValues.String(row, "asset"),
                Started = JsonValues.Int(row, "started"),
                Paid = JsonValues.Int(row, "paid"),
                Abandoned = JsonValues.Int(row, "abandoned"),
            })],
        };
        Attention = new OverviewAttention
        {
            UnderpaidCharges = ChargeAmounts(attention, "underpaid_charges"),
            LatePayments = ChargeAmounts(attention, "late_payments"),
            PayoutsWaitingForAddress = [.. JsonValues.Records(attention, "payouts_waiting_for_address").Select(row => new OverviewWaitingPayouts
            {
                Asset = JsonValues.String(row, "asset"),
                Count = JsonValues.Int(row, "count"),
            })],
            FailingWebhookEndpoints = JsonValues.Int(attention, "failing_webhook_endpoints"),
        };
        Assets = [.. JsonValues.Records(data, "assets").Select(row => new OverviewAsset
        {
            Asset = JsonValues.String(row, "asset"),
            PaidCharges = JsonValues.Int(row, "paid_charges"),
            SharePercent = JsonValues.Decimal(row, "share_percent"),
        })];
        TopPaymentLinks = [.. JsonValues.Records(data, "top_payment_links").Select(row => new OverviewPaymentLink
        {
            Id = JsonValues.NullableString(row, "id"),
            Title = JsonValues.NullableString(row, "title"),
            PaidCharges = JsonValues.Int(row, "paid_charges"),
        })];
        Setup = new OverviewSetup
        {
            PayoutAddress = JsonValues.Bool(setup, "payout_address"),
            PaymentLink = JsonValues.Bool(setup, "payment_link"),
            PaidCharge = JsonValues.Bool(setup, "paid_charge"),
            WebhookEndpoint = JsonValues.Bool(setup, "webhook_endpoint"),
            PublicProfile = JsonValues.Bool(setup, "public_profile"),
        };
        Daily = [.. JsonValues.Records(data, "daily").Select(row => new OverviewDay
        {
            Date = JsonValues.Day(row, "date"),
            Volume = JsonValues.Decimal(row, "volume"),
            SentVolume = JsonValues.Decimal(row, "sent_volume"),
            PaidCharges = JsonValues.Int(row, "paid_charges"),
        })];
        Mode = JsonValues.NullableString(about, "mode");
    }

    /// <summary>The currency the amounts are in: the one most paid charges of the period were in, or the account's default.</summary>
    public string Currency { get; }

    /// <summary>What was paid in the period.</summary>
    public decimal Volume { get; }

    /// <summary>How the volume changed against the 30 days before, in percent, or null when nothing was paid then.</summary>
    public decimal? VolumeChangePercent { get; }

    /// <summary>What the payouts confirmed in the period came to, at their charges' locked rates.</summary>
    public decimal SentVolume { get; }

    /// <summary>How many charges were paid.</summary>
    public int PaidCharges { get; }

    /// <summary>Of the period's charges that were paid, expired or canceled, the share paid, in percent; null when there were none.</summary>
    public decimal? ConversionRatePercent { get; }

    /// <summary>How many charges are waiting to be paid.</summary>
    public int PendingCharges { get; }

    /// <summary>How many payment links take payments.</summary>
    public int ActivePaymentLinks { get; }

    /// <summary>How many invoices are open.</summary>
    public int OpenInvoices { get; }

    /// <summary>How many open invoices are past due.</summary>
    public int OverdueInvoices { get; }

    /// <summary>The first and last day the figures cover.</summary>
    public OverviewPeriod Period { get; }

    /// <summary>How the attempts to pay started in the period ended.</summary>
    public OverviewFunnel Funnel { get; }

    /// <summary>What waits on someone in the account now: short and late payments, payouts with no address, failing webhook endpoints.</summary>
    public OverviewAttention Attention { get; }

    /// <summary>The coins the period's paid charges were paid in, the most used first.</summary>
    public IReadOnlyList<OverviewAsset> Assets { get; }

    /// <summary>The payment links with the most paid charges in the period, up to four.</summary>
    public IReadOnlyList<OverviewPaymentLink> TopPaymentLinks { get; }

    /// <summary>Which of the first steps of setting up the account are done, in the key's mode.</summary>
    public OverviewSetup Setup { get; }

    /// <summary>One entry per day of the period, the oldest first, days without payments included.</summary>
    public IReadOnlyList<OverviewDay> Daily { get; }

    /// <summary>The mode of the API key that fetched the overview, <see cref="VeliraPay.Mode.Live"/> or <see cref="VeliraPay.Mode.Test"/>; null when the answer did not say.</summary>
    public string? Mode { get; }

    /// <summary>Read a list of charges that wait on someone.</summary>
    private static List<OverviewChargeAmount> ChargeAmounts(JsonElement attention, string key) =>
    [
        .. JsonValues.Records(attention, key).Select(row => new OverviewChargeAmount
        {
            Id = JsonValues.String(row, "id"),
            Asset = JsonValues.String(row, "asset"),
            Amount = JsonValues.Decimal(row, "amount"),
            ReceivedAmount = JsonValues.Decimal(row, "received_amount"),
        }),
    ];
}

/// <summary>The days an overview covers.</summary>
public sealed record OverviewPeriod
{
    /// <summary>The first day, or null when the answer has none.</summary>
    public DateOnly? From { get; init; }

    /// <summary>The last day, today, or null when the answer has none.</summary>
    public DateOnly? To { get; init; }
}

/// <summary>How the attempts to pay started in an overview's period ended, each counted once: paid, waiting, abandoned or incomplete.</summary>
public sealed record OverviewFunnel
{
    /// <summary>How many attempts to pay were started; a charge given a new quote or another coin is still the one attempt.</summary>
    public int Started { get; init; }

    /// <summary>How many were paid.</summary>
    public int Paid { get; init; }

    /// <summary>How many are pending or underpaid.</summary>
    public int Waiting { get; init; }

    /// <summary>How many expired or were canceled before any transfer arrived.</summary>
    public int Abandoned { get; init; }

    /// <summary>How many expired or were canceled after a transfer arrived.</summary>
    public int Incomplete { get; init; }

    /// <summary>The attempts per coin, the most started first.</summary>
    public required IReadOnlyList<OverviewFunnelAsset> Assets { get; init; }
}

/// <summary>How the attempts to pay in one coin started in an overview's period ended.</summary>
public sealed record OverviewFunnelAsset
{
    /// <summary>The coin, one of the <see cref="VeliraPay.Asset"/> values.</summary>
    public required string Asset { get; init; }

    /// <summary>How many attempts to pay in it were started.</summary>
    public int Started { get; init; }

    /// <summary>How many were paid.</summary>
    public int Paid { get; init; }

    /// <summary>How many expired or were canceled before any transfer arrived.</summary>
    public int Abandoned { get; init; }
}

/// <summary>What waits on someone in the account now.</summary>
public sealed record OverviewAttention
{
    /// <summary>The latest underpaid charges, up to five, to accept or refund.</summary>
    public required IReadOnlyList<OverviewChargeAmount> UnderpaidCharges { get; init; }

    /// <summary>The latest charges paid after they expired or were canceled, up to five, to accept or refund.</summary>
    public required IReadOnlyList<OverviewChargeAmount> LatePayments { get; init; }

    /// <summary>How many payouts in each coin wait for a payout address.</summary>
    public required IReadOnlyList<OverviewWaitingPayouts> PayoutsWaitingForAddress { get; init; }

    /// <summary>How many active webhook endpoints had a delivery fail in the last 7 days.</summary>
    public int FailingWebhookEndpoints { get; init; }
}

/// <summary>A charge that waits on someone: what it asked for and what arrived.</summary>
public sealed record OverviewChargeAmount
{
    /// <summary>The charge's id.</summary>
    public required string Id { get; init; }

    /// <summary>The coin, one of the <see cref="VeliraPay.Asset"/> values.</summary>
    public required string Asset { get; init; }

    /// <summary>What the charge asked for, in the coin.</summary>
    public decimal Amount { get; init; }

    /// <summary>What arrived, in the coin: for a late payment, the confirmed transfers not counted yet.</summary>
    public decimal ReceivedAmount { get; init; }
}

/// <summary>How many payouts in one coin wait for a payout address.</summary>
public sealed record OverviewWaitingPayouts
{
    /// <summary>The coin, one of the <see cref="VeliraPay.Asset"/> values.</summary>
    public required string Asset { get; init; }

    /// <summary>How many payouts in it wait.</summary>
    public int Count { get; init; }
}

/// <summary>How much of an overview's period's paid charges one coin took.</summary>
public sealed record OverviewAsset
{
    /// <summary>The coin, one of the <see cref="VeliraPay.Asset"/> values.</summary>
    public required string Asset { get; init; }

    /// <summary>How many paid charges were paid in it.</summary>
    public int PaidCharges { get; init; }

    /// <summary>Their share of the period's paid charges, in percent.</summary>
    public decimal SharePercent { get; init; }
}

/// <summary>One of the payment links with the most paid charges in an overview's period.</summary>
public sealed record OverviewPaymentLink
{
    /// <summary>The payment link's id.</summary>
    public string? Id { get; init; }

    /// <summary>Its title.</summary>
    public string? Title { get; init; }

    /// <summary>How many charges made through it were paid in the period.</summary>
    public int PaidCharges { get; init; }
}

/// <summary>Which of the first steps of setting up an account are done, in one mode.</summary>
public sealed record OverviewSetup
{
    /// <summary>Whether a payout address was saved.</summary>
    public bool PayoutAddress { get; init; }

    /// <summary>Whether a payment link was created.</summary>
    public bool PaymentLink { get; init; }

    /// <summary>Whether a charge was paid.</summary>
    public bool PaidCharge { get; init; }

    /// <summary>Whether a webhook endpoint was added.</summary>
    public bool WebhookEndpoint { get; init; }

    /// <summary>Whether the public profile has a logo, a display name or a website.</summary>
    public bool PublicProfile { get; init; }
}

/// <summary>One day of an overview's period.</summary>
public sealed record OverviewDay
{
    /// <summary>The day.</summary>
    public DateOnly? Date { get; init; }

    /// <summary>What was paid that day, in the overview's currency.</summary>
    public decimal Volume { get; init; }

    /// <summary>What the payouts confirmed that day came to, at their charges' locked rates.</summary>
    public decimal SentVolume { get; init; }

    /// <summary>How many charges were paid that day.</summary>
    public int PaidCharges { get; init; }
}
