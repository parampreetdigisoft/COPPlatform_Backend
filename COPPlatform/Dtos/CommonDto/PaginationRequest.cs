namespace COPPlatform.Dtos.CommonDto
{
    public class PaginationRequest
    {
        private int _pageNumber = 1;
        private int _pageSize = 10;

        public int PageNumber
        {
            get => _pageNumber <= 0 ? 1 : _pageNumber;
            set => _pageNumber = value;
        }

        public int PageSize
        {
            get => _pageSize <= 0 ? 10 : _pageSize;
            // Global safety cap to avoid accidental large payloads.
            // This project’s dashboards intentionally cap lists to <= 250.
            set => _pageSize = value > 250 ? 250 : value;
        }

        public string? SortBy { get; set; }
        public string SortDirection { get; set; } = "asc";
        public string? SearchText { get; set; }
        public int? UserId { get; set; } = 0;
    }

    public enum SortDirection
    {
        asc,
        desc
    }
}
