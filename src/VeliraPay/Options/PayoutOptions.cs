namespace VeliraPay;

/// <summary>Which payouts to list.</summary>
public sealed class PayoutListOptions : ListOptions
{
    /// <summary>Only payouts in this state, one of the <see cref="PayoutStatus"/> values.</summary>
    public string? Status { get => Get<string>("status"); set => Set("status", value); }

    /// <summary>Only payouts in this coin.</summary>
    public string? Asset { get => Get<string>("asset"); set => Set("asset", value); }

    /// <summary>A charge id or a transaction hash.</summary>
    public string? Search { get => Get<string>("search"); set => Set("search", value); }
}

/// <summary>Where one network's payouts go.</summary>
public sealed class PayoutAddressUpdateOptions : BaseOptions
{
    /// <summary>The address, on the network named in the request.</summary>
    public required string Address { get => Get<string>("address")!; set => Set("address", value); }
}
