namespace VeliraPay;

/// <summary>The /v1/account and /v1/payment-settings endpoints.</summary>
public sealed class AccountService : Service
{
    internal AccountService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>Retrieve the account the API key belongs to, with the coins it accepts in the key's mode.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Account> RetrieveAsync(CancellationToken cancellationToken = default)
        => Read(await RespondAsync(HttpMethod.Get, "/v1/account", null, null, cancellationToken).ConfigureAwait(false));

    /// <summary>Change the account's name, public profile, checkout and invoice settings or business details, in the shape <see cref="RetrieveAsync"/> reads; null clears a field. Needs the sensitive account:write.</summary>
    /// <param name="options">The changes.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Account> UpdateAsync(AccountUpdateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Read(await RespondAsync(HttpMethod.Patch, "/v1/account", options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Change the default currency, the payment tolerances, the checkout language or invoice reminders. Needs the sensitive payment_settings:write.</summary>
    /// <param name="options">The changes.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Account> UpdatePaymentSettingsAsync(PaymentSettingsUpdateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Read(await RespondAsync(HttpMethod.Patch, "/v1/payment-settings", options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Read the account and its meta from an answer.</summary>
    private static Account Read(ApiResponse response) => new(Data(response), Meta(response));
}
