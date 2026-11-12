namespace VeliraPay;

/// <summary>The /v1/payout-addresses endpoints. Reading needs payout_addresses:read; changing needs the sensitive payout_addresses:write and an owner's switch under Payment settings.</summary>
public sealed class PayoutAddressService : Service
{
    internal PayoutAddressService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>List where payouts go in the API key's mode: one entry per network with coins on offer, its address null while there is none.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<IReadOnlyList<PayoutAddress>> ListAsync(CancellationToken cancellationToken = default)
        => [.. List(await RespondAsync(HttpMethod.Get, "/v1/payout-addresses", null, null, cancellationToken).ConfigureAwait(false)).Select(address => new PayoutAddress(address))];

    /// <summary>
    /// Point the payouts of one network, such as <see cref="AddressNetwork.Bitcoin"/> or <see cref="AddressNetwork.Evm"/>, at an address.
    /// Payouts to a newly saved address wait 48 hours once the account has been paid out. Only a network <see cref="ListAsync"/> returns can be set; the API answers any other with a 404.
    /// </summary>
    /// <param name="network">The network, one of the <see cref="AddressNetwork"/> values, as <see cref="PayoutAddress.Network"/> names it.</param>
    /// <param name="options">The address.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<PayoutAddress> UpdateAsync(string network, PayoutAddressUpdateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new PayoutAddress(await RequestAsync(HttpMethod.Put, Path("/v1/payout-addresses/{id}", network), options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Remove the address of one network, so its payouts wait for a new one. Only a network <see cref="ListAsync"/> returns can be removed; the API answers any other with a 404.</summary>
    /// <param name="network">The network, one of the <see cref="AddressNetwork"/> values, as <see cref="PayoutAddress.Network"/> names it.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<PayoutAddress> DeleteAsync(string network, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Delete, Path("/v1/payout-addresses/{id}", network), null, requestOptions, cancellationToken).ConfigureAwait(false));
}
