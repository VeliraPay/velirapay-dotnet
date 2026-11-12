using System.Text.Json;

namespace VeliraPay;

/// <summary>A crypto payment for a month of Pro: where to pay it and what to send.</summary>
public sealed class ProPayment : ApiResource
{
    /// <summary>Read a Pro payment from the JSON the API sent.</summary>
    /// <param name="json">The payment, as a JSON object.</param>
    /// <param name="meta">What the answer said about it besides: whether this request started it.</param>
    public ProPayment(JsonElement json, JsonElement? meta = null)
        : base(json)
    {
        var data = RawJson;
        var about = meta?.ValueKind == JsonValueKind.Object ? meta.Value : JsonValues.EmptyObject;

        Months = JsonValues.Int(data, "months", 1);
        Price = JsonValues.Decimal(data, "price");
        Currency = JsonValues.String(data, "currency");
        Asset = JsonValues.String(data, "asset");
        AssetAmount = JsonValues.Decimal(data, "asset_amount");
        CheckoutUrl = JsonValues.String(data, "checkout_url");
        ExpiresAt = JsonValues.Date(data, "expires_at");
        Started = JsonValues.Bool(about, "started");
    }

    /// <summary>How many months it pays for.</summary>
    public int Months { get; }

    /// <summary>What it costs, in <see cref="Currency"/>.</summary>
    public decimal Price { get; }

    /// <summary>The currency of the price.</summary>
    public string Currency { get; }

    /// <summary>The coin it is paid in.</summary>
    public string Asset { get; }

    /// <summary>What to send, in the coin.</summary>
    public decimal AssetAmount { get; }

    /// <summary>The page to pay it on.</summary>
    public string CheckoutUrl { get; }

    /// <summary>When its rate stops holding.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>Whether this request started it, rather than answering with the one already waiting.</summary>
    public bool Started { get; }
}
