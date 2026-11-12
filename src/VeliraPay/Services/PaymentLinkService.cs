using System.Runtime.CompilerServices;

namespace VeliraPay;

/// <summary>The /v1/payment-links endpoints.</summary>
public sealed class PaymentLinkService : Service
{
    internal PaymentLinkService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>Create a payment link: one reusable public page, never one per customer. To charge one customer, create a charge.</summary>
    /// <param name="options">The payment link.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<PaymentLink> CreateAsync(PaymentLinkCreateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new PaymentLink(await RequestAsync(HttpMethod.Post, "/v1/payment-links", options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Retrieve a payment link by its id.</summary>
    /// <param name="id">The payment link's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<PaymentLink> RetrieveAsync(string id, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Get, Path("/v1/payment-links/{id}", id), null, null, cancellationToken).ConfigureAwait(false));

    /// <summary>Change any of a payment link's fields: only those set are changed, and null clears one.</summary>
    /// <param name="id">The payment link's id.</param>
    /// <param name="options">The changes.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<PaymentLink> UpdateAsync(string id, PaymentLinkUpdateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new PaymentLink(await RequestAsync(HttpMethod.Patch, Path("/v1/payment-links/{id}", id), options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>List payment links, newest first.</summary>
    /// <param name="options">Filters and the page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<PaymentLink>> ListAsync(PaymentLinkListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync("/v1/payment-links", Query(options), link => new PaymentLink(link), cancellationToken);

    /// <summary>Iterate over every payment link, newest first, fetching pages as they are reached.</summary>
    /// <param name="options">Filters, and how many links to fetch at a time.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<PaymentLink> IteratorAsync(PaymentLinkListOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await ListAsync(options, cancellationToken).ConfigureAwait(false);

        await foreach (var link in page.AutoPagingIteratorAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return link;
        }
    }

    /// <summary>Archive a payment link so it stops taking payments.</summary>
    /// <param name="id">The payment link's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<PaymentLink> ArchiveAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/payment-links/{id}/archive", id), null, requestOptions, cancellationToken).ConfigureAwait(false));

    /// <summary>Restore an archived payment link.</summary>
    /// <param name="id">The payment link's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<PaymentLink> RestoreAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Delete, Path("/v1/payment-links/{id}/archive", id), null, requestOptions, cancellationToken).ConfigureAwait(false));
}
