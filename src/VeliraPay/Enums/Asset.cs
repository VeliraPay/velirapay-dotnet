namespace VeliraPay;

/// <summary>
/// A coin on one particular network, such as <see cref="USDT"/> on Ethereum or <see cref="USDT_TRON"/> on Tron.
/// The account lists the ones it accepts right now; <see cref="AssetService.ListAsync"/> gives them with their minimums.
/// A charge, a payout or a coin can carry a code the API added after this version of the library, so compare with these values rather than assume the list is complete.
/// </summary>
public static class Asset
{
    /// <summary>Bitcoin, on its own chain.</summary>
    public const string BTC = "BTC";

    /// <summary>Bitcoin, paid over the Lightning Network.</summary>
    public const string BTC_LIGHTNING = "BTC_LIGHTNING";

    /// <summary>Litecoin.</summary>
    public const string LTC = "LTC";

    /// <summary>Dogecoin.</summary>
    public const string DOGE = "DOGE";

    /// <summary>Bitcoin Cash.</summary>
    public const string BCH = "BCH";

    /// <summary>Ether, on Ethereum.</summary>
    public const string ETH = "ETH";

    /// <summary>Monero.</summary>
    public const string XMR = "XMR";

    /// <summary>Solana.</summary>
    public const string SOL = "SOL";

    /// <summary>BNB, on BNB Chain.</summary>
    public const string BNB = "BNB";

    /// <summary>TRX, on Tron.</summary>
    public const string TRX = "TRX";

    /// <summary>Tether USD, on Ethereum (ERC-20).</summary>
    public const string USDT = "USDT";

    /// <summary>USD Coin, on Ethereum (ERC-20).</summary>
    public const string USDC = "USDC";

    /// <summary>Tether USD, on Tron (TRC-20).</summary>
    public const string USDT_TRON = "USDT_TRON";

    /// <summary>USD Coin, on Base.</summary>
    public const string USDC_BASE = "USDC_BASE";

    /// <summary>USD Coin, on Polygon.</summary>
    public const string USDC_POLYGON = "USDC_POLYGON";

    /// <summary>Tether USD, on BNB Chain (BEP-20).</summary>
    public const string USDT_BSC = "USDT_BSC";

    /// <summary>USD Coin, on BNB Chain (BEP-20).</summary>
    public const string USDC_BSC = "USDC_BSC";

    /// <summary>Tether USD (USDT0), on Polygon.</summary>
    public const string USDT_POLYGON = "USDT_POLYGON";

    /// <summary>Tether USD (USDT0), on Arbitrum One.</summary>
    public const string USDT_ARBITRUM = "USDT_ARBITRUM";

    /// <summary>USD Coin, on Arbitrum One.</summary>
    public const string USDC_ARBITRUM = "USDC_ARBITRUM";

    /// <summary>Ether, on Base.</summary>
    public const string ETH_BASE = "ETH_BASE";

    /// <summary>Ether, on Arbitrum One.</summary>
    public const string ETH_ARBITRUM = "ETH_ARBITRUM";

    /// <summary>POL, on Polygon.</summary>
    public const string POL = "POL";

    /// <summary>Every code this version of the library knows.</summary>
    internal static readonly string[] Values =
    [
        BTC, BTC_LIGHTNING, LTC, DOGE, BCH, ETH, XMR, SOL, BNB, TRX, USDT, USDC, USDT_TRON, USDC_BASE, USDC_POLYGON,
        USDT_BSC, USDC_BSC, USDT_POLYGON, USDT_ARBITRUM, USDC_ARBITRUM, ETH_BASE, ETH_ARBITRUM, POL,
    ];
}
