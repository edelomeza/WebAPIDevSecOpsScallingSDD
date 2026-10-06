using System.Collections.Generic;

namespace WebAPIDevSecOpsScallingSDD.Dtos
{
    public sealed class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = [];

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }
}
