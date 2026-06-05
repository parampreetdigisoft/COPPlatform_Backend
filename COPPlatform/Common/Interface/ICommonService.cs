using COPPlatform.Dtos.CityDto;
using COPPlatform.Dtos.PillarDto;
using COPPlatform.Models;

namespace COPPlatform.Common.Interface
{
    public interface ICommonService
    {
        /// <summary>
        /// Based on user role it will return pillar wise Manual progress and Ai progress Score
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public Task<List<EvaluationCityProgressResultDto>> GetCitiesProgressAsync(int userId,int role, int year);
        public Task<List<GetCitiesProgressAdminDto>> GetCitiesProgressForAdmin(int userId, int role, int year);
        public Task<List<EvaluationCityProgressResultDto>> GetAssessmentProgressAsync(int userId,int role);
        Task<List<UserEvaluationPillarProgressResultDto>> GetUserProgressByAssessmentId(int? userAssessmentMappingID);
        Task<List<UserEvaluationPillarProgressResultDto>> GetUserProgressByAssessmentIdWeekly(int userAssessmentMappingID,List<string> periods);
        Task<List<AnalyticalLayerSPResult>> GetAnalyticalLayerResultsAsync(
           int userId, int role, int userAssessmentMappingId = 0,
           int pageNumber = 1, int pageSize = 14, int layerId = 0, string search = "");
    }
}
