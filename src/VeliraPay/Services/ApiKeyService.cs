using System.Runtime.CompilerServices;

namespace VeliraPay;

/// <summary>The /v1/api-keys endpoints. Reading needs the sensitive api_keys:read and changing api_keys:write, and a key never reaches further than the key that asks.</summary>
public sealed class ApiKeyService : Service
{
    internal ApiKeyService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>List the account's active keys in the API key's mode, newest first, without the keys themselves.</summary>
    /// <param name="options">The page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<ApiKey>> ListAsync(ListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync("/v1/api-keys", Query(options), key => new ApiKey(key), cancellationToken);

    /// <summary>Iterate over every active key, newest first, fetching pages as they are reached.</summary>
    /// <param name="options">How many keys to fetch at a time.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<ApiKey> IteratorAsync(ListOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await ListAsync(options, cancellationToken).ConfigureAwait(false);

        await foreach (var key in page.AutoPagingIteratorAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return key;
        }
    }

    /// <summary>
    /// Create a key in the API key's mode. Its <see cref="ApiKey.Key"/> holds the key itself, shown this once; leaving the permissions out gives full access.
    /// The answer is never replayed: when it is lost and the request is repeated with the same idempotency key, as a retry is, the call throws a
    /// <see cref="ConflictException"/> whose <see cref="ConflictException.WasReplayed"/> is true. The key was created then, but the key itself is gone:
    /// list the keys, revoke the one you never saw, and create another.
    /// </summary>
    /// <param name="options">The key.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">The key was already created by an earlier request with the same idempotency key, whose answer was lost.</exception>
    public async Task<ApiKey> CreateAsync(ApiKeyCreateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new ApiKey(await RequestAsync(HttpMethod.Post, "/v1/api-keys", options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Change a key's name, permissions or allowed addresses, only those set; null permissions give full access and null addresses anywhere.</summary>
    /// <param name="id">The key's id.</param>
    /// <param name="options">The changes.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<ApiKey> UpdateAsync(string id, ApiKeyUpdateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new ApiKey(await RequestAsync(HttpMethod.Patch, Path("/v1/api-keys/{id}", id), options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Revoke a key, which is refused from its next request.</summary>
    /// <param name="id">The key's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task RevokeAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => RespondAsync(HttpMethod.Delete, Path("/v1/api-keys/{id}", id), null, requestOptions, cancellationToken);
}
