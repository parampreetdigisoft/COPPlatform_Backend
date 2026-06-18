
using COPPlatform.Common.Models;
using COPPlatform.Data;
using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.CityUserDto;
using COPPlatform.Dtos.kpiDto;
using COPPlatform.Enums;
using COPPlatform.IServices;
using COPPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace COPPlatform.Services
{
    public class CityUserService : ICityUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAppLogger _appLogger;
        public CityUserService(ApplicationDbContext context, IAppLogger appLogger)
        {
            _context = context;
            _appLogger = appLogger;
        }



        public async Task<ResultResponseDto<string>> AddCityUserKpisCityAndPillar(AddCityUserKpisCityAndPillar payload, int userId, string tierName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tierName))
                    return ResultResponseDto<string>.Failure(new[] { "Access tier information is missing. Please log in again." });

                if (!Enum.TryParse<TieredAccessPlan>(tierName, true, out var tier))
                    return ResultResponseDto<string>.Failure(new[] { "Invalid tier access. Please contact support team." });

                var tierLimits = tier switch
                {
                    TieredAccessPlan.Basic => new { Min = 3, Max = 7, Name = "Basic" },
                    TieredAccessPlan.Standard => new { Min = 7, Max = 12, Name = "Standard" },
                    TieredAccessPlan.Premium => new { Min = 0, Max = int.MaxValue, Name = "Premium" },
                    _ => new { Min = 0, Max = 0, Name = "Unknown" }
                };

                if (tier != TieredAccessPlan.Premium)
                {
                    bool isValid =
                        payload.Cities.Count >= tierLimits.Min && payload.Cities.Count <= tierLimits.Max &&
                        payload.Pillars.Count >= tierLimits.Min && payload.Pillars.Count <= tierLimits.Max;

                    if (!isValid)
                    {
                        return ResultResponseDto<string>.Failure(new[]
                        {
                            $"Your {tierLimits.Name} plan allows between {tierLimits.Min} and {tierLimits.Max} selections per category (City, and Pillar. Please adjust your selections accordingly."
                        });
                    }
                }

                //  Remove existing mappings
                var existingCities = await _context.PublicUserCityMappings
                    .Where(m => m.UserID == userId)
                    .ToListAsync();

                var existingPillars = await _context.CityUserPillarMappings
                    .Where(m => m.UserID == userId)
                    .ToListAsync();

                _context.PublicUserCityMappings.RemoveRange(existingCities);
                _context.CityUserPillarMappings.RemoveRange(existingPillars);

                var utcNow = DateTime.UtcNow;

                var newCityMappings = payload.Cities.Select(cityId => new PublicUserCityMapping
                {
                    CityID = cityId,
                    UserID = userId,
                    IsActive = true,
                    UpdatedAt = utcNow
                });

                var newPillarMappings = payload.Pillars.Select(pillarId => new CityUserPillarMapping
                {
                    PillarID = pillarId,
                    UserID = userId,
                    IsActive = true,
                    UpdatedAt = utcNow
                });

                await _context.PublicUserCityMappings.AddRangeAsync(newCityMappings);
                await _context.CityUserPillarMappings.AddRangeAsync(newPillarMappings);

                await _context.SaveChangesAsync();

                return ResultResponseDto<string>.Success("", new[] { "Your preferences have been saved successfully." });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in AddCityUserKpisCityAndPillar", ex);
                return ResultResponseDto<string>.Failure(new[]
                {
                    "Something went wrong while saving your selections. Please try again later."
                });
            }
        }
        public async Task<ResultResponseDto<List<GetAllKpisResponseDto>>> GetCityUserKpi(int userId, string tierName)
        {
            try
            {
                var validPillarIds = await _context.CityUserPillarMappings
                    .Where(x => x.IsActive && x.UserID == userId)
                    .Select(x => x.PillarID)
                    .ToListAsync();

                // Step 1: Get valid KPI IDs for this user
                var validKpiIds = await _context.AnalyticalLayerPillarMappings
                    .Where(x => validPillarIds.Contains(x.PillarID))
                    .Select(x => x.LayerID)
                    .Distinct()
                    .ToListAsync();

                if (!validKpiIds.Any())
                {
                    return ResultResponseDto<List<GetAllKpisResponseDto>>.Failure(new List<string> { "you don't have kpi access." });
                }

                // Fetch Analytical Layers that match the user's KPI access
                var result = await _context.AnalyticalLayers
                    .Where(ar => !ar.IsDeleted && validKpiIds.Contains(ar.LayerID))
                    .Select(x=>new GetAllKpisResponseDto
                    {
                        LayerID = x.LayerID,
                        LayerCode = x.LayerCode,
                        LayerName = x.LayerName
                    })
                    .ToListAsync();

                return ResultResponseDto<List<GetAllKpisResponseDto>>.Success(result);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetCityUserKpi", ex);
                return ResultResponseDto<List<GetAllKpisResponseDto>>.Failure(new List<string> { "An error occurred while fetching user KPIs." });
            }
        }
    }
}
