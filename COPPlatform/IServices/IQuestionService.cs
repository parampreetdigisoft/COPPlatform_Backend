using COPPlatform.Common.Models;
using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.CommonDto;
using COPPlatform.Dtos.QuestionDto;
using COPPlatform.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace COPPlatform.IServices
{
    public interface IQuestionService
    {
        Task<List<Pillar>> GetPillarsAsync();
        Task<PaginationResponse<GetQuestionRespones>> GetQuestionsAsync(GetQuestionRequestDto requestDto);
        Task<Question> AddQuestionAsync(Question q);
        Task<ResultResponseDto<string>> AddUpdateQuestion(AddUpdateQuestionDto q);
        Task<ResultResponseDto<string>> AddBulkQuestion(AddBulkQuestionsDto q);
        Task<Question> EditQuestionAsync(int id, Question q);
        Task<bool> DeleteQuestionAsync(int id);
        Task<ResultResponseDto<GetPillarQuestionByCityRespones>> GetQuestionsByAssessmentMappingId(CityPillerRequestDto request, int userId, UserRole userRole);
        Task<Tuple<string,byte[]>> ExportAssessment(int UserAssessmentMappingID, int userId, UserRole userRole);
        Task<ResultResponseDto<List<QuestionsByUserPillarsResponsetDto>>> GetQuestionsHistoryByPillar(GetCityPillarHistoryRequestDto requestDto);
    }
} 