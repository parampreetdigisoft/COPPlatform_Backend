using COPPlatform.Common.Interface;
using COPPlatform.Data;
using COPPlatform.Dtos.CityDto;
using COPPlatform.Dtos.PillarDto;
using COPPlatform.IServices;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace COPPlatform.Common.Implementation
{
    public class CommonService : ICommonService
    {
        #region constructor

        private readonly ApplicationDbContext _context;
        private readonly IAppLogger _appLogger;
        private readonly IWebHostEnvironment _env;
        public CommonService(ApplicationDbContext context, IAppLogger appLogger, IWebHostEnvironment env)
        {
            _context = context;
            _appLogger = appLogger;
            _env = env;
        }
        #endregion

        public async Task<List<EvaluationCityProgressResultDto>> GetCitiesProgressAsync(int userId, int role, int year)
        {
            try
            {
                return await _context.CityProgressResults
                 .FromSqlRaw(
                     "EXEC usp_getCitiesProgressByUserId @userID, @role, @year",
                     new SqlParameter("@userID", userId),
                     new SqlParameter("@role", role),
                     new SqlParameter("@year", year)
                 )
                 .AsNoTracking()
                 .ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing usp_getCitiesProgressByUserId", ex);
                return new List<EvaluationCityProgressResultDto>();
            }
        }
        public async Task<List<GetCitiesProgressAdminDto>> GetCitiesProgressForAdmin(int userId, int role, int year)
        {
            try
            {
                return await _context.GetCitiesProgressAdminDto
                 .FromSqlRaw("EXEC usp_getCitiesProgress_Admin @year",new SqlParameter("@year", year))
                 .AsNoTracking()
                 .ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing usp_getCitiesProgress_Admin", ex);
                return new List<GetCitiesProgressAdminDto>();
            }
        }
        public async Task<List<EvaluationCityProgressResultDto>> GetAssessmentProgressAsync(int userId, int role)
        {
            try
            {
                return await _context.CityProgressResults
                 .FromSqlRaw(
                     "EXEC usp_getAssessmentProgressByUserId @userID, @role",
                     new SqlParameter("@userID", userId),
                     new SqlParameter("@role", role)
                 )
                 .AsNoTracking()
                 .ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing usp_getAssessmentProgressByUserId", ex);
                return new List<EvaluationCityProgressResultDto>();
            }
        }
        public async Task<List<UserEvaluationPillarProgressResultDto>> GetUserProgressByAssessmentId(int userAssessmentMappingID)
        {
            try
            {
                return await _context.UserEvaluationPillarProgressResults
                 .FromSqlRaw(
                     "EXEC usp_getUserProgressByAssessmentId @UserAssessmentMappingID",
                     new SqlParameter("@UserAssessmentMappingID", userAssessmentMappingID)
                 )
                 .AsNoTracking()
                 .ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing usp_getUserProgressByAssessmentId", ex);
                return new List<UserEvaluationPillarProgressResultDto>();
            }
        }

        public async Task<List<UserEvaluationPillarProgressResultDto>> GetUserProgressByAssessmentIdWeekly(
    int userAssessmentMappingID,
    DateTime? week1StartDate = null,
    DateTime? week1EndDate = null,
    DateTime? week2StartDate = null,
    DateTime? week2EndDate = null)
        {
            try
            {
                return await _context.UserEvaluationPillarProgressResults
                    .FromSqlRaw(
                        @"EXEC usp_getWeeklyUserProgressByAssessmentId 
                @UserAssessmentMappingID,
                @Week1StartDate,
                @Week1EndDate,
                @Week2StartDate,
                @Week2EndDate",
                        new SqlParameter("@UserAssessmentMappingID", userAssessmentMappingID),
                        new SqlParameter("@Week1StartDate", (object?)week1StartDate ?? DBNull.Value),
                        new SqlParameter("@Week1EndDate", (object?)week1EndDate ?? DBNull.Value),
                        new SqlParameter("@Week2StartDate", (object?)week2StartDate ?? DBNull.Value),
                        new SqlParameter("@Week2EndDate", (object?)week2EndDate ?? DBNull.Value)
                    )
                    .AsNoTracking()
                    .ToListAsync();

            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing SP", ex);
                return new List<UserEvaluationPillarProgressResultDto>();
            }
        }

    }
}
