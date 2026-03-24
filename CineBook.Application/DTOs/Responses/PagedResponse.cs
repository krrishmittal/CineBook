namespace CineBook.Application.DTOs.Responses
{
    /// <summary>
    /// Wraps a paginated list with metadata for the client.
    /// Returned as: ApiResponse&lt;PagedResponse&lt;T&gt;&gt;
    /// </summary>
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();

        /// <summary>Total records matching the filter (before paging)</summary>
        public int TotalCount { get; set; }

        /// <summary>Current 1-based page number</summary>
        public int Page { get; set; }

        /// <summary>Records per page</summary>
        public int PageSize { get; set; }

        /// <summary>Total pages = ceil(TotalCount / PageSize)</summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}