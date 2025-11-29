namespace Arcana.Core.Common;

/// <summary>
/// Represents a paged result set.
/// 分頁結果集
/// </summary>
/// <typeparam name="T">The type of items</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// The total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 10)
        => new(Array.Empty<T>(), page, pageSize, 0);

    /// <summary>
    /// Maps items to a new type.
    /// </summary>
    public PagedResult<TNew> Map<TNew>(Func<T, TNew> mapper)
        => new(Items.Select(mapper).ToList(), Page, PageSize, TotalCount);
}

/// <summary>
/// Represents paging parameters.
/// 分頁參數
/// </summary>
public record PageRequest(int Page = 1, int PageSize = 10, string? SortBy = null, bool Descending = false)
{
    /// <summary>
    /// The number of items to skip.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}
