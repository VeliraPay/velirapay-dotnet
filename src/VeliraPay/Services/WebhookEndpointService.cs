using System.Runtime.CompilerServices;

namespace VeliraPay;

/// <summary>The /v1/webhook-endpoints endpoints. Reading them needs webhook_endpoints:read, and changing them the sensitive webhook_endpoints:write.</summary>
public sealed class WebhookEndpointService : Service
{
    internal WebhookEndpointService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>List the account's webhook endpoints in the API key's mode, newest first.</summary>
    /// <param name="options">The page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<WebhookEndpoint>> ListAsync(ListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync("/v1/webhook-endpoints", Query(options), endpoint => new WebhookEndpoint(endpoint), cancellationToken);

    /// <summary>Iterate over every webhook endpoint, newest first, fetching pages as they are reached.</summary>
    /// <param name="options">How many endpoints to fetch at a time.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<WebhookEndpoint> IteratorAsync(ListOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await ListAsync(options, cancellationToken).ConfigureAwait(false);

        await foreach (var endpoint in page.AutoPagingIteratorAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return endpoint;
        }
    }

    /// <summary>Retrieve one webhook endpoint.</summary>
    /// <param name="id">The endpoint's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<WebhookEndpoint> RetrieveAsync(string id, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Get, Path("/v1/webhook-endpoints/{id}", id), null, null, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Add an endpoint and send it its verification challenge, which counts against the requests a minute that verifying, testing and retrying share.
    /// The answer carries its <see cref="WebhookEndpoint.Secret"/>, shown this once, and <see cref="WebhookEndpoint.VerificationError"/> while it has not answered.
    /// The answer is never replayed: when it is lost and the request is repeated with the same idempotency key, as a retry is, the call throws a
    /// <see cref="ConflictException"/> whose <see cref="ConflictException.WasReplayed"/> is true. The endpoint was added then, but its secret is gone:
    /// list the endpoints and rotate the new one's secret, or delete it and add it again.
    /// </summary>
    /// <param name="options">The endpoint.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">The endpoint was already added by an earlier request with the same idempotency key, whose answer was lost.</exception>
    public async Task<WebhookEndpoint> CreateAsync(WebhookEndpointCreateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Endpoint(await RespondAsync(HttpMethod.Post, "/v1/webhook-endpoints", options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Change an endpoint's URL, events, description or switch, only those set. A new URL is verified again, with <see cref="WebhookEndpoint.VerificationError"/> while it has not answered.</summary>
    /// <param name="id">The endpoint's id.</param>
    /// <param name="options">The changes.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<WebhookEndpoint> UpdateAsync(string id, WebhookEndpointUpdateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Endpoint(await RespondAsync(HttpMethod.Patch, Path("/v1/webhook-endpoints/{id}", id), options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Delete an endpoint and its delivery history.</summary>
    /// <param name="id">The endpoint's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task DeleteAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => RespondAsync(HttpMethod.Delete, Path("/v1/webhook-endpoints/{id}", id), null, requestOptions, cancellationToken);

    /// <summary>Send the endpoint its verification challenge again. One that does not answer is refused with a <see cref="ValidationException"/> saying what it answered instead.</summary>
    /// <param name="id">The endpoint's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<WebhookEndpoint> VerifyAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/webhook-endpoints/{id}/verify", id), null, requestOptions, cancellationToken).ConfigureAwait(false));

    /// <summary>Queue a sample charge.paid event, marked as a test, to a verified endpoint.</summary>
    /// <param name="id">The endpoint's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<WebhookDelivery> TestAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/webhook-endpoints/{id}/test", id), null, requestOptions, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Replace the endpoint's signing secret. The answer carries the new one, shown this once;
    /// the old one keeps signing for a day, until <see cref="WebhookEndpoint.PreviousSecretExpiresAt"/>.
    /// The answer is never replayed: when it is lost and the request is repeated with the same idempotency key, as a retry is, the call throws a
    /// <see cref="ConflictException"/> whose <see cref="ConflictException.WasReplayed"/> is true. The secret was rotated then, but the new one is gone:
    /// rotate again and put the newest secret in place straight away, since the secret you hold stops signing as soon as you rotate again.
    /// </summary>
    /// <param name="id">The endpoint's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="ConflictException">The secret was already rotated by an earlier request with the same idempotency key, whose answer was lost.</exception>
    public async Task<WebhookEndpoint> RotateSecretAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => Endpoint(await RespondAsync(HttpMethod.Post, Path("/v1/webhook-endpoints/{id}/secret", id), null, requestOptions, cancellationToken).ConfigureAwait(false));

    /// <summary>List what the endpoint was sent, newest first, kept for 30 days, without the bodies; the page's <see cref="Page{T}.AutoPagingIteratorAsync"/> walks the rest.</summary>
    /// <param name="id">The endpoint's id.</param>
    /// <param name="options">Filters and the page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<WebhookDelivery>> DeliveriesAsync(string id, WebhookDeliveryListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync(Path("/v1/webhook-endpoints/{id}/deliveries", id), Query(options), delivery => new WebhookDelivery(delivery), cancellationToken);

    /// <summary>Retrieve one of the endpoint's deliveries with every attempt to send it, the latest first, and the JSON body it carried when the key may also read events.</summary>
    /// <param name="id">The endpoint's id.</param>
    /// <param name="deliveryId">The delivery's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<WebhookDelivery> RetrieveDeliveryAsync(string id, string deliveryId, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Get, Path("/v1/webhook-endpoints/{id}/deliveries/{id}", id, deliveryId), null, null, cancellationToken).ConfigureAwait(false));

    /// <summary>Send one of the endpoint's deliveries again, once the endpoint is verified and not paused.</summary>
    /// <param name="id">The endpoint's id.</param>
    /// <param name="deliveryId">The delivery's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<WebhookDelivery> RetryDeliveryAsync(string id, string deliveryId, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Post, Path("/v1/webhook-endpoints/{id}/deliveries/{id}/retry", id, deliveryId), null, requestOptions, cancellationToken).ConfigureAwait(false));

    /// <summary>Read the endpoint and its meta from an answer.</summary>
    private static WebhookEndpoint Endpoint(ApiResponse response) => new(Data(response), Meta(response));
}
