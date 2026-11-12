using System.Text.Json;

namespace VeliraPay;

/// <summary>A coin that can be paid in the API key's mode, with the lowest price a charge in it can have.</summary>
public sealed class SupportedAsset : ApiResource
{
    /// <summary>Read a coin from the JSON the API sent.</summary>
    /// <param name="json">The coin, as a JSON object.</param>
    public SupportedAsset(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Id = JsonValues.String(data, "id");
        Name = JsonValues.String(data, "name");
        Ticker = JsonValues.String(data, "ticker");
        Network = JsonValues.String(data, "network");
        ContractAddress = JsonValues.NullableString(data, "contract_address");
        Decimals = JsonValues.Int(data, "decimals");
        RequiredConfirmations = JsonValues.Int(data, "required_confirmations");
        ConfirmationWait = JsonValues.String(data, "confirmation_wait");
        Accepted = JsonValues.NullableBool(data, "accepted");
        Currency = JsonValues.String(data, "currency");
        ExchangeRate = JsonValues.NullableDecimal(data, "exchange_rate");
        MinimumAmount = JsonValues.NullableDecimal(data, "minimum_amount");
        MinimumPrice = JsonValues.NullableDecimal(data, "minimum_price");
        NetworkSurcharge = JsonValues.NullableDecimal(data, "network_surcharge");
        Asset = Array.IndexOf(VeliraPay.Asset.Values, Id) >= 0 ? Id : null;
    }

    /// <summary>What the API calls the coin, such as "BTC" or "USDT_BSC": give it as the asset when you create a charge.</summary>
    public string Id { get; }

    /// <summary>The coin's name, such as "Tether USD".</summary>
    public string Name { get; }

    /// <summary>The coin's symbol, such as "USDT".</summary>
    public string Ticker { get; }

    /// <summary>The network the customer has to send it on.</summary>
    public string Network { get; }

    /// <summary>The token's contract on that network, or null for a network's own coin.</summary>
    public string? ContractAddress { get; }

    /// <summary>The decimal places the coin has on its network.</summary>
    public int Decimals { get; }

    /// <summary>How many confirmations a payment needs before it counts.</summary>
    public int RequiredConfirmations { get; }

    /// <summary>About how long those confirmations take, in words.</summary>
    public string ConfirmationWait { get; }

    /// <summary>Whether the account takes it right now, which needs a payout address for its network; null when the answer does not say.</summary>
    public bool? Accepted { get; }

    /// <summary>The currency <see cref="ExchangeRate"/> and <see cref="MinimumPrice"/> are in.</summary>
    public string Currency { get; }

    /// <summary>What one unit of the coin is worth in that currency right now, or null while no rate is available.</summary>
    public decimal? ExchangeRate { get; }

    /// <summary>The least a charge in it can come to, in the coin itself, or null when it has no minimum of its own.</summary>
    public decimal? MinimumAmount { get; }

    /// <summary>The lowest price a charge in it can have right now, in that currency, or null while a coin with a minimum has no rate.</summary>
    public decimal? MinimumPrice { get; }

    /// <summary>What a charge in the coin asks the customer for on top of the price, in the coin, for the network; null for a coin without one.</summary>
    public decimal? NetworkSurcharge { get; }

    /// <summary>The <see cref="VeliraPay.Asset"/> value of the coin, or null for one the API added after this version of the library.</summary>
    public string? Asset { get; }
}
