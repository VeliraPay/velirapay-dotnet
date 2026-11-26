using System.Text.Json;

namespace VeliraPay;

/// <summary>A refund started for a charge: what it sets aside, where it goes and how far it has got. VeliraPay never sends it; you do, from your own wallet.</summary>
public sealed class RefundRequest : ApiResource
{
    /// <summary>Read a refund request from the JSON the API sent.</summary>
    /// <param name="json">The refund request, as a JSON object.</param>
    public RefundRequest(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Id = JsonValues.String(data, "id", JsonValues.String(data, "code"));
        Charge = JsonValues.Reference(data, "charge") ?? string.Empty;
        Status = JsonValues.String(data, "status");
        Amount = JsonValues.Decimal(data, "amount");
        Reason = JsonValues.NullableString(data, "reason");
        Address = JsonValues.NullableString(data, "address");
        AddressGivenBy = JsonValues.NullableString(data, "address_given_by");
        AddressUrl = JsonValues.NullableString(data, "address_url");
        LinkExpiresAt = JsonValues.Date(data, "link_expires_at");
        EmailedAt = JsonValues.Date(data, "emailed_at");
        Txid = JsonValues.NullableString(data, "txid");
        DetectedAmount = JsonValues.NullableDecimal(data, "detected_amount");
        Confirmations = JsonValues.NullableInt(data, "confirmations");
        Refund = JsonValues.NullableObject(data, "refund", refund => new Refund(refund));
        CreatedAt = JsonValues.Date(data, "created_at");
        CompletedAt = JsonValues.Date(data, "completed_at");
        CanceledAt = JsonValues.Date(data, "canceled_at");
    }

    /// <summary>The refund request's id.</summary>
    public string Id { get; }

    /// <summary>The id of the charge being refunded.</summary>
    public string Charge { get; }

    /// <summary>One of the <see cref="RefundRequestStatus"/> values, or one the API added after this version of the library.</summary>
    public string Status { get; }

    /// <summary>What the refund sets aside, in the charge's coin.</summary>
    public decimal Amount { get; }

    /// <summary>Why the refund is made.</summary>
    public string? Reason { get; }

    /// <summary>Where the refund goes, once the customer or you gave it.</summary>
    public string? Address { get; }

    /// <summary>Who gave the address, "customer" or "merchant", or null while there is none.</summary>
    public string? AddressGivenBy { get; }

    /// <summary>The signed page the customer gives the address on, while the refund still takes one; null once it is given or the link lapsed.</summary>
    public string? AddressUrl { get; }

    /// <summary>When <see cref="AddressUrl"/> stops working.</summary>
    public DateTimeOffset? LinkExpiresAt { get; }

    /// <summary>When the link was last emailed to the customer, or null when it never was.</summary>
    public DateTimeOffset? EmailedAt { get; }

    /// <summary>The transaction you said carried the refund, or the one the chain showed.</summary>
    public string? Txid { get; }

    /// <summary>What the chain shows arriving at the address for the refund, in the charge's coin.</summary>
    public decimal? DetectedAmount { get; }

    /// <summary>How many confirmations that transfer has.</summary>
    public int? Confirmations { get; }

    /// <summary>The refund it was recorded as, once it was.</summary>
    public Refund? Refund { get; }

    /// <summary>When the refund was started.</summary>
    public DateTimeOffset? CreatedAt { get; }

    /// <summary>When it was recorded as a refund.</summary>
    public DateTimeOffset? CompletedAt { get; }

    /// <summary>When it was canceled.</summary>
    public DateTimeOffset? CanceledAt { get; }

    /// <summary>Whether the refund still sets money aside: it waits for the address, for your transfer or for the chain to confirm it.</summary>
    public bool IsOpen => Status is RefundRequestStatus.AwaitingAddress or RefundRequestStatus.AwaitingTransfer or RefundRequestStatus.Confirming;

    /// <summary>Whether the refund was recorded.</summary>
    public bool IsRefunded => Status == RefundRequestStatus.Refunded;
}
