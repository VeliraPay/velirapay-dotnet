using System.Runtime.CompilerServices;

namespace VeliraPay;

/// <summary>The /v1/invitations endpoints. Reading needs the sensitive members:read and changing members:write.</summary>
public sealed class InvitationService : Service
{
    internal InvitationService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>List the invitations nobody has accepted yet, newest first, expired ones included.</summary>
    /// <param name="options">The page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<Invitation>> ListAsync(ListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync("/v1/invitations", Query(options), invitation => new Invitation(invitation), cancellationToken);

    /// <summary>Iterate over every waiting invitation, newest first, fetching pages as they are reached.</summary>
    /// <param name="options">How many invitations to fetch at a time.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<Invitation> IteratorAsync(ListOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await ListAsync(options, cancellationToken).ConfigureAwait(false);

        await foreach (var invitation in page.AutoPagingIteratorAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return invitation;
        }
    }

    /// <summary>Email someone an invitation to join as an "admin", "finance", "support" or "viewer", in the name of whoever made the API key.</summary>
    /// <param name="options">The invitation.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Invitation> CreateAsync(InvitationCreateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new Invitation(await RequestAsync(HttpMethod.Post, "/v1/invitations", options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Withdraw an invitation, so its link stops working.</summary>
    /// <param name="id">The invitation's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task CancelAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => RespondAsync(HttpMethod.Delete, Path("/v1/invitations/{id}", id), null, requestOptions, cancellationToken);
}
