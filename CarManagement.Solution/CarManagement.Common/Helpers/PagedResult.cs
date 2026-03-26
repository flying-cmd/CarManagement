namespace CarManagement.Common.Helpers;

/// <summary>
/// Represents a paged result.
/// </summary>
/// <typeparam name="T">The type of the items in the paged result.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>
    /// The current page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// The total number of items accross all pages.
    /// </summary>
    public int TotalCount { get; set; }
}
