using COPPlatform.Common.Interface;
using COPPlatform.Data;
using COPPlatform.Dtos.CityDto;
using COPPlatform.Dtos.PillarDto;
using COPPlatform.IServices;
using COPPlatform.Models;
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
        
        public async Task<List<AnalyticalLayerSPResult>> GetAnalyticalLayerResultsAsync(
            int userId, int role, int userAssessmentMappingId = 0,
            int pageNumber = 1, int pageSize = 14, int layerId = 0, string search = "")
        {
            try
            {
                return await _context.AnalyticalLayerSPResults
                    .FromSqlRaw(
                        "EXEC usp_GetAnalyticalLayerResults @UserID, @Role, @UserAssessmentMappingID, @PageNumber, @PageSize, @LayerID, @Search",
                        new SqlParameter("@UserID", userId),
                        new SqlParameter("@Role", role),
                        new SqlParameter("@UserAssessmentMappingID", userAssessmentMappingId),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@LayerID", layerId),
                        new SqlParameter("@Search", search ?? string.Empty)
                    )
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing usp_GetAnalyticalLayerResults", ex);
                return new List<AnalyticalLayerSPResult>();
            }
        }
        public async Task<List<PillarAssessmentProgressResult>> GetPillarAssessmentProgressResults(int userId, int role, int userAssessmentMappingId = 0)

        {
            try
            {
                return await _context.PillarAssessmentProgressResults
                    .FromSqlRaw(
                        "EXEC usp_getPillarAssessmentProgressByUserId @UserID, @Role, @UserAssessmentMappingID",
                        new SqlParameter("@UserID", userId),
                        new SqlParameter("@Role", role),
                        new SqlParameter("@UserAssessmentMappingID", userAssessmentMappingId)
                    )
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing usp_GetAnalyticalLayerResults", ex);
                return new List<PillarAssessmentProgressResult>();
            }
        }
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
        public async Task<List<UserEvaluationPillarProgressResultDto>> GetUserProgressByAssessmentId(int? userAssessmentMappingID)
        {
            try
            {
                return await _context.UserEvaluationPillarProgressResults
                 .FromSqlRaw(
                     "EXEC usp_getUserProgressByAssessmentId @UserAssessmentMappingID",
                    new SqlParameter("@UserAssessmentMappingID",(object?)userAssessmentMappingID ?? DBNull.Value)
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
              List<string> periods
        )
        {
            try
            {
                var periodsCsv = string.Join(",", periods);

                return await _context.UserEvaluationPillarProgressResults
                    .FromSqlRaw(
                        @"EXEC usp_getWeeklyUserProgressByAssessmentId 
                  @UserAssessmentMappingID,
                  @Periods",
                        new SqlParameter("@UserAssessmentMappingID", userAssessmentMappingID),
                        new SqlParameter("@Periods", periodsCsv)
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
