namespace VeliraPay;

/// <summary>The /v1/overview endpoint. Needs overview:read.</summary>
public sealed class OverviewService : Service
{
    internal OverviewService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>Retrieve the dashboard's figures for the last 30 days, in the API key's mode.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Overview> RetrieveAsync(CancellationToken cancellationToken = default)
        => new(await RequestAsync(HttpMethod.Get, "/v1/overview", null, null, cancellationToken).ConfigureAwait(false));
}
