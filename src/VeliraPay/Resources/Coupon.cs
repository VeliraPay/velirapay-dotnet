using System.Text.Json;

namespace VeliraPay;

/// <summary>A discount code customers enter at checkout, or that is applied for them through a payment link's URL or the API.</summary>
public sealed class Coupon : ApiResource
{
    /// <summary>Read a coupon from the JSON the API sent.</summary>
    /// <param name="json">The coupon, as a JSON object.</param>
    public Coupon(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Code = JsonValues.String(data, "code", JsonValues.String(data, "id"));
        Id = JsonValues.String(data, "id", Code);
        PercentOff = JsonValues.NullableDecimal(data, "percent_off");
        AmountOff = JsonValues.NullableDecimal(data, "amount_off");
        Currency = JsonValues.NullableString(data, "currency");
        MinAmount = JsonValues.NullableDecimal(data, "min_amount");
        MaxRedemptions = JsonValues.NullableInt(data, "max_redemptions");
        TimesRedeemed = JsonValues.Int(data, "times_redeemed");
        OncePerCustomer = JsonValues.Bool(data, "once_per_customer");
        PaymentLinks = JsonValues.Strings(data, "payment_links");
        Active = JsonValues.Bool(data, "active");
        ExpiresAt = JsonValues.Date(data, "expires_at");
        CreatedAt = JsonValues.Date(data, "created_at");
        UpdatedAt = JsonValues.Date(data, "updated_at");
    }

    /// <summary>The coupon's id, which is its code.</summary>
    public string Id { get; }

    /// <summary>The code customers type, in capitals; it is matched in any case.</summary>
    public string Code { get; }

    /// <summary>The share taken off, such as 10.00, or null for an amount off.</summary>
    public decimal? PercentOff { get; }

    /// <summary>The amount taken off, or null for a percentage.</summary>
    public decimal? AmountOff { get; }

    /// <summary>The only fiat currency the coupon works in, or null for a percentage that works in every one.</summary>
    public string? Currency { get; }

    /// <summary>The least the price before the discount must be.</summary>
    public decimal? MinAmount { get; }

    /// <summary>How many payments may use the coupon, or null for no limit.</summary>
    public int? MaxRedemptions { get; }

    /// <summary>How many paid charges used the coupon.</summary>
    public int TimesRedeemed { get; }

    /// <summary>Whether each customer email can use the coupon only once.</summary>
    public bool OncePerCustomer { get; }

    /// <summary>The ids of the payment links the coupon is limited to; empty for every link, and charges without one.</summary>
    public IReadOnlyList<string> PaymentLinks { get; }

    /// <summary>Whether customers can use the coupon, rather than it being paused.</summary>
    public bool Active { get; }

    /// <summary>When the coupon stops working.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>When the coupon was created.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>When the coupon was last changed.</summary>
    public DateTimeOffset? UpdatedAt { get; }

    /// <summary>Whether the coupon takes a percentage off rather than a fixed amount.</summary>
    public bool IsPercentage => PercentOff is not null;
}
