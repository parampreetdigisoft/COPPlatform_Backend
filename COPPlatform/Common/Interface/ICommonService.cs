using COPPlatform.Dtos.CityDto;
using COPPlatform.Dtos.PillarDto;

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
        Task<List<UserEvaluationPillarProgressResultDto>> GetUserProgressByAssessmentId(int userAssessmentMappingID);
        Task<List<UserEvaluationPillarProgressResultDto>> GetUserProgressByAssessmentIdWeekly(int userAssessmentMappingID,
    DateTime? week1StartDate = null, DateTime? week1EndDate = null, DateTime? week2StartDate = null, DateTime? week2EndDate = null);
    }
}
