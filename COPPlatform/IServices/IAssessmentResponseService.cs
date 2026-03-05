using COPPlatform.Common.Models;
using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.CommonDto;
using COPPlatform.Dtos.dashboard;
using COPPlatform.Models;

namespace COPPlatform.IServices
{
    public interface IAssessmentResponseService
    {
        Task<List<AssessmentResponse>> GetAllAsync();
        Task<AssessmentResponse> GetByIdAsync(int id);
        Task<AssessmentResponse> AddAsync(AssessmentResponse response);
        Task<AssessmentResponse> UpdateAsync(int id, AssessmentResponse response);
        Task<bool> DeleteAsync(int id);
        Task<ResultResponseDto<string>> SaveAssessment(AddAssessmentDto request, int userID, UserRole userRole);
        Task<PaginationResponse<GetCityAssessmentResponseDto>> GetAssessmentResult(GetAssessmentRequestDto request, UserRole role);
        Task<PaginationResponse<GetAssessmentQuestionResponseDto>> GetAssessmentQuestion(GetAssessmentQuestoinRequestDto request);
        Task<ResultResponseDto<string>> ImportAssessmentAsync(IFormFile file,int userID, UserRole userRole);
        Task<GetCityQuestionHistoryReponseDto> GetCityQuestionHistory(UserCityRequstDto userCityRequstDto);
        Task<ResultResponseDto<GetAssessmentHistoryDto>> GetAssessmentProgressHistory(int assessmentID, int userID, UserRole userRole);
        Task<ResultResponseDto<string>> ChangeAssessmentStatus(ChangeAssessmentStatusRequestDto r);
        Task<ResultResponseDto<string>> TransferAssessment(TransferAssessmentRequestDto r);
        Task<ResultResponseDto<AiCityPillarDashboardResponseDto>> GetCityPillarHistory(UserCityDashBoardRequstDto userCityRequstDto,int userID, UserRole userRole);
        Task<ResultResponseDto<List<GetAssignedAssessmentResponseDto>>> GetAssignedAssessments(int userID, UserRole userRole);
        Task<ResultResponseDto<List<GetAssignedAssessmentResponseDto>>> GetAssignedInvitations(int userID, UserRole userRole);
    }
} 