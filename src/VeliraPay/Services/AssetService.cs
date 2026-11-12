namespace VeliraPay;

/// <summary>The /v1/assets endpoint.</summary>
public sealed class AssetService : Service
{
    internal AssetService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>
    /// List the coins that can be paid in the API key's mode, with the lowest price each can be charged at, in one page,
    /// whose <see cref="Page{T}.Currencies"/> are the currencies prices can be given in.
    /// </summary>
    /// <param name="options">The currency of the prices.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<SupportedAsset>> ListAsync(AssetListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync("/v1/assets", Query(options), asset => new SupportedAsset(asset), cancellationToken);
}
