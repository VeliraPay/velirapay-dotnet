using System.Runtime.CompilerServices;

namespace VeliraPay;

/// <summary>The /v1/coupons endpoints, where a coupon's id is its code.</summary>
public sealed class CouponService : Service
{
    internal CouponService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>Create a coupon taking either a percentage or an amount off; a currency is required with an amount off or a minimum amount.</summary>
    /// <param name="options">The coupon.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Coupon> CreateAsync(CouponCreateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new Coupon(await RequestAsync(HttpMethod.Post, "/v1/coupons", options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Retrieve a coupon by its code, in any case.</summary>
    /// <param name="id">The coupon's code.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Coupon> RetrieveAsync(string id, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Get, Path("/v1/coupons/{id}", id), null, null, cancellationToken).ConfigureAwait(false));

    /// <summary>Change a coupon's limits; its code, discount, currency and minimum price cannot change.</summary>
    /// <param name="id">The coupon's code.</param>
    /// <param name="options">The changes.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Coupon> UpdateAsync(string id, CouponUpdateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new Coupon(await RequestAsync(HttpMethod.Patch, Path("/v1/coupons/{id}", id), options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>List coupons, newest first.</summary>
    /// <param name="options">Filters and the page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<Coupon>> ListAsync(CouponListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync("/v1/coupons", Query(options), coupon => new Coupon(coupon), cancellationToken);

    /// <summary>Iterate over every coupon, newest first, fetching pages as they are reached.</summary>
    /// <param name="options">Filters, and how many coupons to fetch at a time.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<Coupon> IteratorAsync(CouponListOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await ListAsync(options, cancellationToken).ConfigureAwait(false);

        await foreach (var coupon in page.AutoPagingIteratorAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return coupon;
        }
    }

    /// <summary>Delete a coupon; charges that used it keep its code and their discount.</summary>
    /// <param name="id">The coupon's code.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task DeleteAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => RespondAsync(HttpMethod.Delete, Path("/v1/coupons/{id}", id), null, requestOptions, cancellationToken);
}
