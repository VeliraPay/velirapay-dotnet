namespace VeliraPay;

/// <summary>How to list the coins on offer.</summary>
public sealed class AssetListOptions : BaseOptions
{
    /// <summary>The currency of the prices; the account's default when left out.</summary>
    public string? Currency { get => Get<string>("currency"); set => Set("currency", value); }
}
