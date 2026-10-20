namespace Forgotten.Nft.Application.Common;

/// <summary>
/// A bounded page of results. Pagination is always bounded to avoid downloading
/// an entire wallet/collection into memory (spec §5.3, §6.2).
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    /// <summary>Total item count when the provider reports one; otherwise null (do not fabricate — spec §6.3).</summary>
    public int? TotalItems { get; init; }

    public static PagedResult<T> Empty(int page, int pageSize) =>
        new() { Items = Array.Empty<T>(), Page = page, PageSize = pageSize, TotalItems = 0 };
}
