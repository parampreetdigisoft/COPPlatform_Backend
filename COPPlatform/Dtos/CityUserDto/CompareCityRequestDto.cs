using COPPlatform.Dtos.CommonDto;

namespace COPPlatform.Dtos.CityUserDto
{
    public class CompareCityRequestDto : PaginationRequest
    {
        public List<int> Cities { get; set; }
        public List<int> Kpis { get; set; } = new();
        public DateTime UpdatedAt { get; set; } = new DateTime(DateTime.UtcNow.Year, 1, 1);
    }

}
