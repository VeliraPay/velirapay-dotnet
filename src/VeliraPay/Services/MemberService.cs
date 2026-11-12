using System.Runtime.CompilerServices;

namespace VeliraPay;

/// <summary>The /v1/members endpoints. Reading needs the sensitive members:read; changing a role or removing someone needs members:write on a key an owner made.</summary>
public sealed class MemberService : Service
{
    internal MemberService(HttpTransport transport)
        : base(transport)
    {
    }

    /// <summary>List the account's members, oldest first.</summary>
    /// <param name="options">The page to fetch.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<Member>> ListAsync(ListOptions? options = null, CancellationToken cancellationToken = default)
        => PageAsync("/v1/members", Query(options), member => new Member(member), cancellationToken);

    /// <summary>Iterate over every member, oldest first, fetching pages as they are reached.</summary>
    /// <param name="options">How many members to fetch at a time.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<Member> IteratorAsync(ListOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await ListAsync(options, cancellationToken).ConfigureAwait(false);

        await foreach (var member in page.AutoPagingIteratorAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return member;
        }
    }

    /// <summary>Give a member another role: "admin", "finance", "support" or "viewer". An owner is never changed this way.</summary>
    /// <param name="id">The member's id.</param>
    /// <param name="options">The role.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Member> UpdateAsync(string id, MemberUpdateOptions options, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new Member(await RequestAsync(HttpMethod.Patch, Path("/v1/members/{id}", id), options, requestOptions, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Remove a member, which also revokes the API keys they made. An owner is never removed this way.</summary>
    /// <param name="id">The member's id.</param>
    /// <param name="requestOptions">An idempotency key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task RemoveAsync(string id, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        => RespondAsync(HttpMethod.Delete, Path("/v1/members/{id}", id), null, requestOptions, cancellationToken);
}
