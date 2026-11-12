namespace VeliraPay;

/// <summary>
/// The networks an account keeps one payout address for, as <see cref="PayoutAddress.Network"/> names them and
/// <see cref="PayoutAddressService.UpdateAsync"/> and <see cref="PayoutAddressService.DeleteAsync"/> take them.
/// Only the networks <see cref="PayoutAddressService.ListAsync"/> returns, those with coins on offer in the API key's mode, can be set or removed;
/// the API answers any other with a 404.
/// </summary>
public static class AddressNetwork
{
    /// <summary>Bitcoin, for <see cref="Asset.BTC"/>.</summary>
    public const string Bitcoin = "bitcoin";

    /// <summary>Litecoin, for <see cref="Asset.LTC"/>.</summary>
    public const string Litecoin = "litecoin";

    /// <summary>Dogecoin, for <see cref="Asset.DOGE"/>.</summary>
    public const string Dogecoin = "dogecoin";

    /// <summary>Bitcoin Cash, for <see cref="Asset.BCH"/>.</summary>
    public const string BitcoinCash = "bitcoincash";

    /// <summary>Ethereum (0x… addresses), for <see cref="Asset.ETH"/>, <see cref="Asset.USDT"/> and <see cref="Asset.USDC"/>.</summary>
    public const string Evm = "evm";

    /// <summary>BNB Chain (0x… addresses), for <see cref="Asset.BNB"/>, <see cref="Asset.USDT_BSC"/> and <see cref="Asset.USDC_BSC"/>; live mode only.</summary>
    public const string Bsc = "bsc";

    /// <summary>Base (0x… addresses), for <see cref="Asset.ETH_BASE"/> and <see cref="Asset.USDC_BASE"/>; live mode only.</summary>
    public const string Base = "base";

    /// <summary>Arbitrum One (0x… addresses), for <see cref="Asset.ETH_ARBITRUM"/>, <see cref="Asset.USDT_ARBITRUM"/> and <see cref="Asset.USDC_ARBITRUM"/>; live mode only.</summary>
    public const string Arbitrum = "arbitrum";

    /// <summary>Polygon (0x… addresses), for <see cref="Asset.POL"/>, <see cref="Asset.USDT_POLYGON"/> and <see cref="Asset.USDC_POLYGON"/>; live mode only.</summary>
    public const string Polygon = "polygon";

    /// <summary>Tron, for <see cref="Asset.TRX"/> and <see cref="Asset.USDT_TRON"/>.</summary>
    public const string Tron = "tron";

    /// <summary>Monero, for <see cref="Asset.XMR"/>.</summary>
    public const string Monero = "monero";

    /// <summary>Solana, for <see cref="Asset.SOL"/>.</summary>
    public const string Solana = "solana";

    /// <summary>A Lightning address (name@domain), for <see cref="Asset.BTC_LIGHTNING"/>.</summary>
    public const string Lightning = "lightning";
}
