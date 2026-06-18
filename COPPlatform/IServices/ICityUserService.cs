using COPPlatform.Common.Models;
using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.CityUserDto;
using COPPlatform.Dtos.kpiDto;

namespace COPPlatform.IServices
{
    public interface ICityUserService
    {
        Task<ResultResponseDto<string>> AddCityUserKpisCityAndPillar(AddCityUserKpisCityAndPillar payload,int userID, string tierName);
        Task<ResultResponseDto<List<GetAllKpisResponseDto>>> GetCityUserKpi(int userID, string tierName);
    }
}
