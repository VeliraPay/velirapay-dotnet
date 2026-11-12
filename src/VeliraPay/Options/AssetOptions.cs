namespace VeliraPay;

/// <summary>How to list the coins on offer.</summary>
public sealed class AssetListOptions : BaseOptions
{
    /// <summary>The currency of the prices, one of the <see cref="VeliraPay.Currency"/> values; the account's default when left out.</summary>
    public string? Currency { get => Get<string>("currency"); set => Set("currency", value); }
}
