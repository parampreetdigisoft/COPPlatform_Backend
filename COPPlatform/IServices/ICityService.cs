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
        Task<PaginationResponse<CityResponseDto>> GetCitiesAsync(PaginationRequest request, UserRole userRole);
        Task<ResultResponseDto<List<UserCityMappingResponseDto>>> getAllCityByUserId(int userId, UserRole userRole);
        Task<ResultResponseDto<City>> GetByIdAsync(int id);
        Task<ResultResponseDto<string>> AddBulkCityAsync(BulkAddCityDto q, string image = "");
        Task<ResultResponseDto<City>> EditCityAsync(int id, AddUpdateCityDto q);
        Task<ResultResponseDto<bool>> DeleteCityAsync(int id);
        Task<ResultResponseDto<object>> AssingCityToUser(int userId, int cityId, int AssignedByUserId);
        Task<ResultResponseDto<object>> EditAssingCity(int id,int userId, int cityId, int AssignedByUserId);
        Task<ResultResponseDto<object>> UnAssignCity(UserCityUnMappingRequestDto requestDto);
        Task<ResultResponseDto<List<UserCityMappingResponseDto>>> GetCityByUserIdForAssessment(int userId);
        Task<ResultResponseDto<CityHistoryDto>> GetCityHistory(int userID, DateTime updatedA, UserRole userRole);
        Task<ResultResponseDto<List<GetCitiesSubmitionHistoryReponseDto>>> GetCitiesProgressByUserId(int userID, DateTime updateAt, UserRole userRole);
        Task<ResultResponseDto<string>> AddUpdateCity(AddUpdateCityDto q);
        Task<ResultResponseDto<List<UserCityMappingResponseDto>>> getAllCityByLocation(GetNearestCityRequestDto r);
        Task<ResultResponseDto<List<UserCityMappingResponseDto>>> GetAiAccessCity(int userId, UserRole userRole);
        Task<ResultResponseDto<byte[]>> ExportCities(int userId, UserRole userRole);
        Task<ResultResponseDto<CardDetailsDto>> GetCardDetails(int userID, UserRole userRole);
        Task<ResultResponseDto<CardDetailsDto>> GetExecutiveCardDetails(int userID, UserRole userRole);

    }
}
