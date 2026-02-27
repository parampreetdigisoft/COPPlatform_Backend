using COPPlatform.Common.Models;
using COPPlatform.Dtos.AiDto;
using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.CityDto;
using COPPlatform.Dtos.CityUserDto;
using COPPlatform.Dtos.CommonDto;
using COPPlatform.Dtos.kpiDto;
using COPPlatform.Dtos.PublicDto;
using COPPlatform.Enums;
using COPPlatform.Models;

namespace COPPlatform.IServices
{
    public interface ICityUserService
    {
        Task<ResultResponseDto<List<PartnerCityResponseDto>>> GetCityUserCities(int userID);
        Task<ResultResponseDto<CityHistoryDto>> GetCityHistory(int userId, TieredAccessPlan tier);
        Task<ResultResponseDto<List<GetCitiesSubmitionHistoryReponseDto>>> GetCitiesProgressByUserId(int userID);
        Task<GetCityQuestionHistoryReponseDto> GetCityQuestionHistory(UserCityRequstDto userCityRequstDto);
        Task<PaginationResponse<CityResponseDto>> GetCitiesAsync(PaginationRequest request);
        Task<ResultResponseDto<CityDetailsDto>> GetCityDetails(UserCityRequstDto userCityRequstDto);
        Task<ResultResponseDto<List<CityPillarQuestionDetailsDto>>> GetCityPillarDetails(UserCityGetPillarInfoRequstDto userCityRequstDto);
        Task<ResultResponseDto<string>> AddCityUserKpisCityAndPillar(AddCityUserKpisCityAndPillar payload,int userID, string tierName);
        Task<ResultResponseDto<List<GetAllKpisResponseDto>>> GetCityUserKpi(int userID, string tierName);
        Task<ResultResponseDto<CompareCityResponseDto>> CompareCities(CompareCityRequestDto c, int userId, string tierName);
        Task<ResultResponseDto<AiCityPillarReponseDto>> GetAICityPillars(AiCityPillarRequestDto r, int userID, string tierName);
    }
}
