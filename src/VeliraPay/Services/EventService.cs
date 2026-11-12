using System.Runtime.CompilerServices;

namespace VeliraPay;

/// <summary>The /v1/events endpoints.</summary>
public sealed class EventService : Service
{
    internal EventService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>Retrieve an event by its id.</summary>
    /// <param name="id">The event's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Event> RetrieveAsync(string id, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Get, Path("/v1/events/{id}", id), null, null, cancellationToken).ConfigureAwait(false));

    /// <summary>List events, newest first.</summary>
    /// <param name="options">Filters and the page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<Event>> ListAsync(EventListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync("/v1/events", Query(options), item => new Event(item), cancellationToken);

    /// <summary>Iterate over every event, newest first, fetching pages as they are reached.</summary>
    /// <param name="options">Filters, and how many events to fetch at a time.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<Event> IteratorAsync(EventListOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await ListAsync(options, cancellationToken).ConfigureAwait(false);

        await foreach (var item in page.AutoPagingIteratorAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }
}
