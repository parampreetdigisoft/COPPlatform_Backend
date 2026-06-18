using COPPlatform.Common.Models;
using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.CityDto;
using COPPlatform.Dtos.CommonDto;
using COPPlatform.Dtos.dashboard;
using COPPlatform.Models;

namespace COPPlatform.IServices
{
    public interface ICityService
    {

        Task<ResultResponseDto<object>> AssingCityToUser(int userId, int cityId, int AssignedByUserId);
        Task<ResultResponseDto<object>> EditAssingCity(int id,int userId, int cityId, int AssignedByUserId);
        Task<ResultResponseDto<object>> UnAssignCity(UserCityUnMappingRequestDto requestDto);       
        Task<ResultResponseDto<byte[]>> ExportCities(int userId, UserRole userRole);
        Task<ResultResponseDto<CardDetailsDto>> GetCardDetails(int userID, UserRole userRole);
        Task<ResultResponseDto<CardDetailsDto>> GetExecutiveCardDetails(int userID, UserRole userRole);

    }
}
