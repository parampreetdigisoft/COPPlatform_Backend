using COPPlatform.Common.Models;
using COPPlatform.Dtos.CommonDto;
using COPPlatform.Dtos.PublicDto;

namespace COPPlatform.IServices
{
    public interface IPublicService
    {
        Task<ResultResponseDto<List<PartnerCityResponseDto>>> GetAllCities();
        Task<ResultResponseDto<PartnerCityFilterResponse>> GetPartnerCitiesFilterRecord();
        Task<ResultResponseDto<List<PillarResponseDto>>> GetAllPillarAsync();
        Task<PaginationResponse<PartnerCityResponseDto>> GetPartnerCities(PartnerCityRequestDto r);
        Task<CountryCityResponse> GetCountriesAndCities_WithStaleSupport();
        Task<ResultResponseDto<List<PromotedPillarsResponseDto>>> GetPromotedCities();
    }
}
