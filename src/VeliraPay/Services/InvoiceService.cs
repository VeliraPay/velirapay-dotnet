using System.Runtime.CompilerServices;

namespace VeliraPay;

/// <summary>The /v1/invoices endpoints.</summary>
public sealed class InvoiceService : Service
{
    internal InvoiceService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>Create an invoice, for a total or a list of items, and optionally email it to the customer.</summary>
    /// <param name="options">The invoice.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Invoice> CreateAsync(InvoiceCreateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new Invoice(await RequestAsync(HttpMethod.Post, "/v1/invoices", options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Retrieve an invoice by its id.</summary>
    /// <param name="id">The invoice's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Invoice> RetrieveAsync(string id, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Get, Path("/v1/invoices/{id}", id), null, null, cancellationToken).ConfigureAwait(false));

    /// <summary>List invoices, newest first.</summary>
    /// <param name="options">Filters and the page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<Invoice>> ListAsync(InvoiceListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync("/v1/invoices", Query(options), invoice => new Invoice(invoice), cancellationToken);

    /// <summary>Iterate over every invoice, newest first, fetching pages as they are reached.</summary>
    /// <param name="options">Filters, and how many invoices to fetch at a time.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<Invoice> IteratorAsync(InvoiceListOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await ListAsync(options, cancellationToken).ConfigureAwait(false);

        await foreach (var invoice in page.AutoPagingIteratorAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return invoice;
        }
    }

    /// <summary>Email an open invoice to its customer, as a reminder when it was sent before.</summary>
    /// <param name="id">The invoice's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Invoice> SendAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/invoices/{id}/send", id), null, requestOptions, cancellationToken).ConfigureAwait(false));

    /// <summary>Void an open invoice so it can no longer be paid.</summary>
    /// <param name="id">The invoice's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Invoice> VoidAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/invoices/{id}/void", id), null, requestOptions, cancellationToken).ConfigureAwait(false));
}
