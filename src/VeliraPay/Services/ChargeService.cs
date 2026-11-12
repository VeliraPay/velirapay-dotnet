using System.Runtime.CompilerServices;

namespace VeliraPay;

/// <summary>The /v1/charges endpoints.</summary>
public sealed class ChargeService : Service
{
    internal ChargeService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>Charge a customer: create a charge with what you already know about them, and send them to its <see cref="Charge.CheckoutUrl"/>.</summary>
    /// <param name="options">The charge.</param>
    /// <param name="requestOptions">An idempotency key, such as your order number, to make the call safe to repeat.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Charge> CreateAsync(ChargeCreateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new Charge(await RequestAsync(HttpMethod.Post, "/v1/charges", options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Retrieve a charge by its id.</summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Charge> RetrieveAsync(string id, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Get, Path("/v1/charges/{id}", id), null, null, cancellationToken).ConfigureAwait(false));

    /// <summary>List charges, newest first.</summary>
    /// <param name="options">Filters and the page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<Charge>> ListAsync(ChargeListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync("/v1/charges", Query(options), charge => new Charge(charge), cancellationToken);

    /// <summary>Iterate over every charge, newest first, fetching pages as they are reached.</summary>
    /// <param name="options">Filters, and how many charges to fetch at a time.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<Charge> IteratorAsync(ChargeListOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await ListAsync(options, cancellationToken).ConfigureAwait(false);

        await foreach (var charge in page.AutoPagingIteratorAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return charge;
        }
    }

    /// <summary>Cancel a pending charge that no transfer has been seen for, so it can no longer be paid.</summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Charge> CancelAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/charges/{id}/cancel", id), null, requestOptions, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Record a refund you sent the customer from your own wallet. An underpaid charge or a late payment is accepted first,
    /// and the network surcharge is never refundable; the API answers 422 otherwise.
    /// </summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="options">The refund.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Charge> RecordRefundAsync(string id, RefundOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new Charge(await RequestAsync(HttpMethod.Post, Path("/v1/charges/{id}/refunds", id), options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Email the customer the payment page of a pending charge.</summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Charge> SendAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/charges/{id}/send", id), null, requestOptions, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Accept an underpaid charge, or a late payment on an expired or canceled one, as paid for what arrived and confirmed.
    /// The API answers 409 for any other charge, and for an underpaid one while a payment to it is still confirming.
    /// </summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Charge> AcceptAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/charges/{id}/accept", id), null, requestOptions, cancellationToken).ConfigureAwait(false));

    /// <summary>Pay a test-mode charge without coins, in full or by a chosen amount in the charge's coin. The webhooks go out as for a real payment.</summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="options">How much arrives; the whole amount due when null.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Charge> SimulateAsync(string id, ChargeSimulateOptions? options = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/charges/{id}/simulate", id), options is { Parameters.Count: > 0 } ? options : null, requestOptions, cancellationToken).ConfigureAwait(false));
}
