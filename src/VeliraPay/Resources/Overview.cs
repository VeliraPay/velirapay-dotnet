using System.Text.Json;

namespace VeliraPay;

/// <summary>The dashboard's figures for the last 30 days. Everything else it carries, such as the funnel and what needs attention, is read with <see cref="ApiResource.Get"/>.</summary>
public sealed class Overview : ApiResource
{
    /// <summary>Read the overview from the JSON the API sent.</summary>
    /// <param name="json">The overview, as a JSON object.</param>
    public Overview(JsonElement json)
        : base(json)
    {
        var data = RawJson;

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
    }

    /// <summary>The currency the amounts are in.</summary>
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
}
