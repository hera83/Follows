namespace web.Infrastructure
{
    public static class PaginationHelper
    {
        public static List<int?> BuildPageNumbers(int currentPage, int totalPages, int siblingCount = 1)
        {
            var pages = new List<int?>();
            if (totalPages <= 0)
            {
                return pages;
            }

            var windowStart = Math.Max(1, currentPage - siblingCount);
            var windowEnd = Math.Min(totalPages, currentPage + siblingCount);

            pages.Add(1);

            if (windowStart > 2)
            {
                pages.Add(null);
            }

            for (var page = Math.Max(2, windowStart); page <= Math.Min(totalPages - 1, windowEnd); page++)
            {
                pages.Add(page);
            }

            if (windowEnd < totalPages - 1)
            {
                pages.Add(null);
            }

            if (totalPages > 1)
            {
                pages.Add(totalPages);
            }

            return pages;
        }
    }
}
