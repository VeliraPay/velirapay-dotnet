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
    /// <param name="requestOptions">
    /// An idempotency key, such as your order number and the attempt ("order-1042-attempt-1"), to make the call safe to repeat.
    /// A new attempt, such as replacing a charge that expired, needs a new key.
    /// </param>
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

    /// <summary>List the refunds started for a charge, newest first; the page's <see cref="Page{T}.AutoPagingIteratorAsync"/> walks the rest.</summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="options">The page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<RefundRequest>> RefundRequestsAsync(string id, ListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync(Path("/v1/charges/{id}/refund-requests", id), Query(options), refundRequest => new RefundRequest(refundRequest), cancellationToken);

    /// <summary>Retrieve one of a charge's refund requests.</summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="refundRequestId">The refund request's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<RefundRequest> RetrieveRefundRequestAsync(string id, string refundRequestId, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Get, Path("/v1/charges/{id}/refund-requests/{id}", id, refundRequestId), null, null, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Start a refund: set the amount aside and, without an address, email the customer a link to give one, which is the request's
    /// <see cref="RefundRequest.AddressUrl"/> either way. You then send it from your own wallet, and it is recorded once the chain shows it.
    /// An underpaid charge or a late payment is accepted first, and the network surcharge is never refundable; the API answers 422 otherwise.
    /// </summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="options">The refund.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<RefundRequest> StartRefundAsync(string id, RefundRequestCreateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new RefundRequest(await RequestAsync(HttpMethod.Post, Path("/v1/charges/{id}/refund-requests", id), options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Give a refund waiting for the customer's address the one they gave you some other way. The API answers 409 once the refund no longer takes one.</summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="refundRequestId">The refund request's id.</param>
    /// <param name="options">The address.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<RefundRequest> SetRefundAddressAsync(string id, string refundRequestId, RefundAddressOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new RefundRequest(await RequestAsync(HttpMethod.Post, Path("/v1/charges/{id}/refund-requests/{id}/address", id, refundRequestId), options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Give a refund the hash of the transaction you sent it in. The chain is looked at straight away, and the refund is recorded once the transfer
    /// has its confirmations. The API answers 409 unless the refund waits for your transfer or is confirming, and 422 for a hash already taken.
    /// </summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="refundRequestId">The refund request's id.</param>
    /// <param name="options">The transaction.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<RefundRequest> MarkRefundSentAsync(string id, string refundRequestId, RefundSentOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new RefundRequest(await RequestAsync(HttpMethod.Post, Path("/v1/charges/{id}/refund-requests/{id}/sent", id, refundRequestId), options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Record a started refund as sent, on your word, for a transfer the chain is not seen to carry. The API answers 409 once the refund is no longer open,
    /// and 422 for a hash already taken.
    /// </summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="refundRequestId">The refund request's id.</param>
    /// <param name="options">The transaction that carried it, when there is one.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<RefundRequest> CompleteRefundAsync(string id, string refundRequestId, RefundCompleteOptions? options = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/charges/{id}/refund-requests/{id}/complete", id, refundRequestId), options is { Parameters.Count: > 0 } ? options : null, requestOptions, cancellationToken).ConfigureAwait(false));

    /// <summary>Email the customer the link to give a refund's address again. The API answers 409 unless the refund waits for the address, and when the email could not go.</summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="refundRequestId">The refund request's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<RefundRequest> ResendRefundLinkAsync(string id, string refundRequestId, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/charges/{id}/refund-requests/{id}/resend", id, refundRequestId), null, requestOptions, cancellationToken).ConfigureAwait(false));

    /// <summary>Stop a refund that has not been recorded, which frees what it set aside. The API answers 409 once the refund is no longer open.</summary>
    /// <param name="id">The charge's id.</param>
    /// <param name="refundRequestId">The refund request's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<RefundRequest> CancelRefundAsync(string id, string refundRequestId, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/charges/{id}/refund-requests/{id}/cancel", id, refundRequestId), null, requestOptions, cancellationToken).ConfigureAwait(false));

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
