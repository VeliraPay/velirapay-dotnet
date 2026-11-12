namespace VeliraPay;

/// <summary>How to pay for a month of Pro.</summary>
public sealed class ProPaymentOptions : BaseOptions
{
    /// <summary>The coin to pay in.</summary>
    public required string Asset { get => Get<string>("asset")!; set => Set("asset", value); }
}
