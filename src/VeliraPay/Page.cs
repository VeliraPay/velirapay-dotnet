using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace VeliraPay;

/// <summary>One page of a list, able to fetch the pages after it. Enumerating it gives the objects on this page alone; <see cref="AutoPagingIteratorAsync"/> walks the rest.</summary>
/// <typeparam name="T">The kind of object the list holds.</typeparam>
public sealed class Page<T> : IReadOnlyList<T>
    where T : ApiResource
{
    private readonly Func<int, CancellationToken, Task<Page<T>>> fetch;

    /// <summary>Create a page, which fetches the others of its list by their number.</summary>
    internal Page(IReadOnlyList<T> data, int currentPage, int lastPage, int perPage, int total, Func<int, CancellationToken, Task<Page<T>>> fetch)
    {
        Data = data;
        CurrentPage = currentPage;
        LastPage = lastPage;
        PerPage = perPage;
        Total = total;
        this.fetch = fetch;
    }

    /// <summary>The objects on this page.</summary>
    public IReadOnlyList<T> Data { get; }

    /// <summary>The number of this page, from 1.</summary>
    public int CurrentPage { get; }

    /// <summary>The number of the last page.</summary>
    public int LastPage { get; }

    /// <summary>How many objects a page holds.</summary>
    public int PerPage { get; }

    /// <summary>How many objects the whole list holds.</summary>
    public int Total { get; }

    /// <summary>Whether there are pages after this one.</summary>
    public bool HasMore => CurrentPage < LastPage;

    /// <summary>Whether this page holds no objects.</summary>
    public bool IsEmpty => Data.Count == 0;

    /// <summary>How many objects this page holds.</summary>
    public int Count => Data.Count;

    /// <summary>The object at a position on this page.</summary>
    /// <param name="index">The position, from 0.</param>
    public T this[int index] => Data[index];

    /// <summary>Fetch the next page, or null when this is the last.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<Page<T>?> NextPageAsync(CancellationToken cancellationToken = default)
        => HasMore ? await fetch(CurrentPage + 1, cancellationToken).ConfigureAwait(false) : null;

    /// <summary>Iterate over the objects on this page and every page after it, fetching each as it is reached.</summary>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<T> AutoPagingIteratorAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var page = this;

        while (true)
        {
            foreach (var item in page.Data)
            {
                // A record created while paging pushes older ones onto the next page, so they can come round twice.
                if (item.Get("id") is { ValueKind: JsonValueKind.String } id && !seen.Add(id.GetString()!))
                {
                    continue;
                }

                yield return item;
            }

            var next = await page.NextPageAsync(cancellationToken).ConfigureAwait(false);

            if (next is null || next.CurrentPage <= page.CurrentPage)
            {
                yield break;
            }

            page = next;
        }
    }

    /// <summary>Iterate over the objects on this page.</summary>
    public IEnumerator<T> GetEnumerator() => Data.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
