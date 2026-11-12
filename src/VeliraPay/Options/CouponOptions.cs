namespace VeliraPay;

/// <summary>A new coupon, taking either <see cref="PercentOff"/> or <see cref="AmountOff"/>; a currency is required with an amount off or a minimum amount.</summary>
public sealed class CouponCreateOptions : BaseOptions
{
    /// <summary>The code customers type: 3 to 32 letters, digits, dashes or underscores, stored in capitals.</summary>
    public required string Code { get => Get<string>("code")!; set => Set("code", value); }

    /// <summary>The share taken off, from 0.01 to 99.99.</summary>
    public decimal? PercentOff { get => Get<decimal?>("percent_off"); set => Set("percent_off", value); }

    /// <summary>The amount taken off, in <see cref="Currency"/>.</summary>
    public decimal? AmountOff { get => Get<decimal?>("amount_off"); set => Set("amount_off", value); }

    /// <summary>The only fiat currency the coupon works in.</summary>
    public string? Currency { get => Get<string>("currency"); set => Set("currency", value); }

    /// <summary>The least the price before the discount must be, in <see cref="Currency"/>.</summary>
    public decimal? MinAmount { get => Get<decimal?>("min_amount"); set => Set("min_amount", value); }

    /// <summary>How many payments may use the coupon; null for no limit.</summary>
    public int? MaxRedemptions { get => Get<int?>("max_redemptions"); set => Set("max_redemptions", value); }

    /// <summary>Whether each customer email can use the coupon only once.</summary>
    public bool? OncePerCustomer { get => Get<bool?>("once_per_customer"); set => Set("once_per_customer", value); }

    /// <summary>The ids of the payment links the coupon is limited to; empty or null for every link, and charges made without one.</summary>
    public IReadOnlyList<string>? PaymentLinks { get => Get<IReadOnlyList<string>>("payment_links"); set => Set("payment_links", value); }

    /// <summary>Whether customers can use the coupon, rather than it being paused.</summary>
    public bool? Active { get => Get<bool?>("active"); set => Set("active", value); }

    /// <summary>When the coupon stops working.</summary>
    public DateTimeOffset? ExpiresAt { get => Get<DateTimeOffset?>("expires_at"); set => Set("expires_at", value); }
}

/// <summary>Changes to a coupon's limits; its code, discount, currency and minimum price cannot change.</summary>
public sealed class CouponUpdateOptions : BaseOptions
{
    /// <summary>How many payments may use the coupon; null for no limit.</summary>
    public int? MaxRedemptions { get => Get<int?>("max_redemptions"); set => Set("max_redemptions", value); }

    /// <summary>Whether each customer email can use the coupon only once.</summary>
    public bool? OncePerCustomer { get => Get<bool?>("once_per_customer"); set => Set("once_per_customer", value); }

    /// <summary>The ids of the payment links the coupon is limited to, replacing the whole list; empty or null for every link.</summary>
    public IReadOnlyList<string>? PaymentLinks { get => Get<IReadOnlyList<string>>("payment_links"); set => Set("payment_links", value); }

    /// <summary>Whether customers can use the coupon, rather than it being paused.</summary>
    public bool? Active { get => Get<bool?>("active"); set => Set("active", value); }

    /// <summary>When the coupon stops working; null for never.</summary>
    public DateTimeOffset? ExpiresAt { get => Get<DateTimeOffset?>("expires_at"); set => Set("expires_at", value); }
}

/// <summary>Which coupons to list.</summary>
public sealed class CouponListOptions : ListOptions
{
    /// <summary>Part of a coupon's code.</summary>
    public string? Search { get => Get<string>("search"); set => Set("search", value); }
}
