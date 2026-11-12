using System.Runtime.CompilerServices;

namespace VeliraPay;

/// <summary>The /v1/payouts endpoints. They only read: payouts go out on their own.</summary>
public sealed class PayoutService : Service
{
    internal PayoutService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>Retrieve the payout of a charge, by the charge's id; the API answers 404 until the charge is paid.</summary>
    /// <param name="charge">The charge's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Payout> RetrieveAsync(string charge, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Get, Path("/v1/payouts/{id}", charge), null, null, cancellationToken).ConfigureAwait(false));

    /// <summary>List payouts, newest first.</summary>
    /// <param name="options">Filters and the page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<Payout>> ListAsync(PayoutListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync("/v1/payouts", Query(options), payout => new Payout(payout), cancellationToken);

    /// <summary>Iterate over every payout, newest first, fetching pages as they are reached.</summary>
    /// <param name="options">Filters, and how many payouts to fetch at a time.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<Payout> IteratorAsync(PayoutListOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await ListAsync(options, cancellationToken).ConfigureAwait(false);

        await foreach (var payout in page.AutoPagingIteratorAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return payout;
        }
    }

    /// <summary>Add up, per coin, what has been paid out, what is on its way and what is still owed.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<IReadOnlyList<PayoutSummary>> SummaryAsync(CancellationToken cancellationToken = default)
        => (await PageAsync("/v1/payouts/summary", [], row => new PayoutSummary(row), cancellationToken).ConfigureAwait(false)).Data;
}
