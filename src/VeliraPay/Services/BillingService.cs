namespace VeliraPay;

/// <summary>The /v1/billing endpoints. Needs billing:write and a live key.</summary>
public sealed class BillingService : Service
{
    internal BillingService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>Start paying for a month of Pro in a coin, answering with the checkout to pay it on, or with the payment already waiting in that coin.</summary>
    /// <param name="options">The coin to pay in.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<ProPayment> StartProPaymentAsync(ProPaymentOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var response = await RespondAsync(HttpMethod.Post, "/v1/billing/pro", options, requestOptions, cancellationToken).ConfigureAwait(false);

        return new ProPayment(Data(response), Meta(response));
    }
}
