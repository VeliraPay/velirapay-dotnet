using System.Runtime.CompilerServices;

namespace VeliraPay;

/// <summary>The /v1/activity endpoint. Needs the sensitive activity:read.</summary>
public sealed class ActivityService : Service
{
    internal ActivityService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>List the account's activity log, newest first: the API key's mode, and what belongs to none, such as sign-ins.</summary>
    /// <param name="options">Filters and the page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<Activity>> ListAsync(ActivityListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync("/v1/activity", Query(options), entry => new Activity(entry), cancellationToken);

    /// <summary>Iterate over the whole activity log, newest first, fetching pages as they are reached.</summary>
    /// <param name="options">Filters, and how many entries to fetch at a time.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<Activity> IteratorAsync(ActivityListOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await ListAsync(options, cancellationToken).ConfigureAwait(false);

        await foreach (var entry in page.AutoPagingIteratorAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return entry;
        }
    }
}
