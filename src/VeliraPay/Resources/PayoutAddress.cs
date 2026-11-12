using System.Text.Json;

namespace VeliraPay;

/// <summary>Where the account's payouts on one network go.</summary>
public sealed class PayoutAddress : ApiResource
{
    /// <summary>Read a payout address from the JSON the API sent.</summary>
    /// <param name="json">The payout address, as a JSON object.</param>
    public PayoutAddress(JsonElement json)
        : base(json)
    {
        var data = RawJson;

        Network = JsonValues.String(data, "network");
        Name = JsonValues.String(data, "name");
        Address = JsonValues.NullableString(data, "address");
        Assets = JsonValues.Strings(data, "assets");
        PayoutsHeldUntil = JsonValues.Date(data, "payouts_held_until");
        UpdatedAt = JsonValues.Date(data, "updated_at");
    }

    /// <summary>The network, such as "bitcoin" or "evm", which is also its id in the paths.</summary>
    public string Network { get; }

    /// <summary>The network's name.</summary>
    public string Name { get; }

    /// <summary>Where payouts go, or null while there is no address: payouts in its coins then wait for one.</summary>
    public string? Address { get; }

    /// <summary>The coins on offer on this network.</summary>
    public IReadOnlyList<string> Assets { get; }

    /// <summary>Until when payouts to a newly saved address wait.</summary>
    public DateTimeOffset? PayoutsHeldUntil { get; }

    /// <summary>When the address was last saved.</summary>
    public DateTimeOffset? UpdatedAt { get; }
}
