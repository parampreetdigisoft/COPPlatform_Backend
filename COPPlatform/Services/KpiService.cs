using COPPlatform.Common.Implementation;
using COPPlatform.Common.Interface;
using COPPlatform.Common.Models;
using COPPlatform.Data;
using COPPlatform.Dtos.CityUserDto;
using COPPlatform.Dtos.CommonDto;
using COPPlatform.Dtos.kpiDto;
using COPPlatform.Enums;
using COPPlatform.IServices;
using COPPlatform.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace COPPlatform.Services
{
    public class KpiService : IKpiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAppLogger _appLogger;
        private readonly ICommonService _commonService;
        public KpiService(ApplicationDbContext context, IAppLogger appLogger, ICommonService commonService)
        {
            _context = context;
            _appLogger = appLogger;
            _commonService = commonService;
        }

        #region GetAnalyticalLayerResults

        public async Task<PaginationResponse<GetAnalyticalLayerResultDto>> 
            GetAnalyticalLayerResults(GetAnalyticalLayerRequestDto request, int userId, UserRole role)
        {
            try
            {
                var layerScores = await _commonService.GetAnalyticalLayerResultsAsync(
                    userId, (int)role, request.UserAssessmentMappingID, request.PageNumber,
                    request.PageSize, request.LayerID.GetValueOrDefault(), request.SearchText ?? "");

                var results = await MapLayerScoresAsync(layerScores);
                var totalRecords = layerScores.Any() ? layerScores.Max(x => x.TotalRecords) : 0;

                return new PaginationResponse<GetAnalyticalLayerResultDto>
                {
                    Data = results,
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetAnalyticalLayers", ex);
                return new PaginationResponse<GetAnalyticalLayerResultDto>();
            }
        }

        public async Task<ResultResponseDto<GetKpiLayerChartResponseDto>> GetKpiLayerChart(
            GetKpiLayerChartRequestDto request, int userId, UserRole role)
        {
            try
            {
                var layerIds = request.LayerIDs?.Where(x => x > 0).Distinct().ToList() ?? new List<int>();
                var spLayerId = layerIds.Count == 1 ? layerIds[0] : 0;
                var pageNumber = layerIds.Count > 1 ? 1 : request.PageNumber;
                var pageSize = layerIds.Count > 1 ? 200 : request.PageSize;

                var layerScores = await _commonService.GetAnalyticalLayerResultsAsync(
                    userId, (int)role, request.UserAssessmentMappingID, pageNumber,
                    pageSize, spLayerId, request.SearchText ?? "");

                if (layerIds.Count > 1)
                {
                    layerScores = layerScores
                        .Where(x => layerIds.Contains(x.LayerID))
                        .ToList();
                }

                var items = await MapLayerScoresAsync(layerScores);
                var totalRecords = layerIds.Count > 1
                    ? items.Count
                    : layerScores.Any() ? layerScores.Max(x => x.TotalRecords) : 0;

                var seriesName = items.FirstOrDefault()?.GeographicReference ?? "KPI Progress";

                var response = new GetKpiLayerChartResponseDto
                {
                    Categories = items.Select(x => $"{x.LayerCode} - {x.LayerName}").ToList(),
                    Series = new List<KpiLayerChartSeriesDto>
                    {
                        new()
                        {
                            Name = seriesName,
                            Data = items.Select(x => Math.Round(x.CalValue ?? 0, 2)).ToList()
                        }
                    },
                    Items = items,
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    AverageScore = items.Any()
                        ? Math.Round(items.Average(x => x.CalValue ?? 0), 2)
                        : 0
                };

                return ResultResponseDto<GetKpiLayerChartResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetKpiLayerChart", ex);
                return ResultResponseDto<GetKpiLayerChartResponseDto>.Failure(
                    new List<string> { "An error occurred while loading KPI chart data." });
            }
        }

        private async Task<List<GetAnalyticalLayerResultDto>> MapLayerScoresAsync(
            List<AnalyticalLayerSPResult> layerScores)
        {
            if (!layerScores.Any())
                return new List<GetAnalyticalLayerResultDto>();

            var layerIDs = layerScores.Select(x => x.LayerID).Distinct().ToList();

            var analyticalLayers = await _context.AnalyticalLayers
                .AsNoTracking()
                .Include(c => c.FiveLevelInterpretations)
                .Where(x => !x.IsDeleted && layerIDs.Contains(x.LayerID))
                .Select(x => new GetAnalyticalLayerResultDto
                {
                    LayerID = x.LayerID,
                    LayerCode = x.LayerCode,
                    LayerName = x.LayerName,
                    Purpose = x.Purpose,
                    CalText = x.CalText,
                    FiveLevelInterpretations = x.FiveLevelInterpretations
                })
                .ToListAsync();

            var results = new List<GetAnalyticalLayerResultDto>();

            foreach (var score in layerScores)
            {
                var layer = analyticalLayers.FirstOrDefault(x => x.LayerID == score.LayerID);
                if (layer == null) continue;

                results.Add(new GetAnalyticalLayerResultDto
                {
                    UserAssessmentMappingID = score.UserAssessmentMappingID,
                    GeographicReference = score.GeographicReference,
                    InterpretationID = score.InterpretationID,
                    CalValue = score.CalValue,
                    PillarID = score.PillarID,
                    PillarName = score.PillarName,
                    LayerID = layer.LayerID,
                    LayerCode = layer.LayerCode,
                    LayerName = layer.LayerName,
                    Purpose = layer.Purpose,
                    CalText = layer.CalText,
                    FiveLevelInterpretations = layer.FiveLevelInterpretations
                });
            }

            return results;
        }

        #endregion
        public async Task<ResultResponseDto<List<AnalyticalLayer>>> GetAllKpi()
        {
            try
            {
                var result = await _context.AnalyticalLayers
                    .Where(ar => !ar.IsDeleted)
                    .ToListAsync();
                    
                 return ResultResponseDto<List<AnalyticalLayer>>.Success(result); 
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetAnalyticalLayers", ex);
                return  ResultResponseDto<List<AnalyticalLayer>>.Failure(new List<string> { "an error occure"});
            }
        }
        public async Task<ResultResponseDto<CompareCityResponseDto>> CompareCities(CompareCityRequestDto c, int userId, UserRole role)
        {
            var chartRequest = new GetKpiLayerChartRequestDto
            {
                UserAssessmentMappingID = c.UserAssessmentMappingID ?? 0,
                LayerIDs = c.Kpis ?? new List<int>(),
                PageNumber = c.PageNumber,
                PageSize = c.PageSize,
                SearchText = c.SearchText
            };

            var chartResult = await GetKpiLayerChart(chartRequest, userId, role);
            if (!chartResult.Succeeded || chartResult.Result == null)
            {
                return ResultResponseDto<CompareCityResponseDto>.Failure(
                    chartResult.Errors?.ToList() ?? new List<string> { "An error occurred while comparing KPI layers." });
            }

            var chart = chartResult.Result;
            var assessmentName = chart.Items.FirstOrDefault()?.GeographicReference ?? "Assessment";

            var response = new CompareCityResponseDto
            {
                Categories = chart.Items.Select(x => x.LayerCode).ToList(),
                Series = new List<ChartSeriesDto>
                {
                    new()
                    {
                        Name = assessmentName,
                        Data = chart.Series.FirstOrDefault()?.Data ?? new List<decimal>(),
                        AiData = new List<decimal>()
                    }
                },
                TableData = chart.Items.Select(item => new ChartTableRowDto
                {
                    LayerID = item.LayerID,
                    LayerCode = item.LayerCode,
                    LayerName = item.LayerName,
                    PeerCityScore = item.CalValue ?? 0,
                    CityValues = new List<CityValueDto>
                    {
                        new()
                        {
                            CityID = item.UserAssessmentMappingID,
                            CityName = assessmentName,
                            Value = item.CalValue ?? 0,
                            AiValue = 0
                        }
                    }
                }).ToList()
            };

            return ResultResponseDto<CompareCityResponseDto>.Success(response);
        }

        public async Task<ResultResponseDto<GetMutiplekpiLayerResultsDto>> GetMutiplekpiLayerResults(
            GetMutiplekpiLayerRequestDto request,
            int userId,
            UserRole role,
            TieredAccessPlan userPlan = TieredAccessPlan.Pending)
        {
            try
            {
                var year = request.Year;
                var startDate = new DateTime(year, 1, 1);
                var endDate = startDate.AddYears(1);

                if (role == UserRole.Executive)
                {
                    var validCityIds = await _context.PublicUserCityMappings
                        .Where(x =>
                            x.IsActive &&
                            x.UserID == userId)
                        .Select(x => x.CityID)
                        .ToListAsync();

                    bool hasInvalidCity = request.CityIDs
                        .Any(cityId => !validCityIds.Contains(cityId));

                    if (hasInvalidCity)
                    {
                        return ResultResponseDto<GetMutiplekpiLayerResultsDto>
                            .Failure(new List<string> { "You are not authorized to access one or more selected cities." });
                    }
                }


                var query = _context.AnalyticalLayerResults
                    .AsNoTracking()
                    .Where(x =>
                        request.CityIDs.Contains(x.CityID) &&
                        x.LayerID == request.LayerID &&
                        (
                            (x.LastUpdated >= startDate && x.LastUpdated < endDate) ||
                            (x.AiLastUpdated >= startDate && x.AiLastUpdated < endDate)
                        ));

                var response = await query
                    .GroupBy(x => x.LayerID)
                    .Select(g => new GetMutiplekpiLayerResultsDto
                    {
                        LayerID = g.Key,

                        //LayerCode = g.Select(x => x.AnalyticalLayer.LayerCode).FirstOrDefault()?? string.Empty,
                        //LayerName = g.Select(x => x.AnalyticalLayer.LayerName).FirstOrDefault() ?? string.Empty,
                        //Purpose = g.Select(x => x.AnalyticalLayer.Purpose).FirstOrDefault() ?? string.Empty,
                        //CalText1 = g.Select(x => x.AnalyticalLayer.CalText1).FirstOrDefault(),
                        //CalText2 = g.Select(x => x.AnalyticalLayer.CalText2).FirstOrDefault(),
                        //CalText3 = g.Select(x => x.AnalyticalLayer.CalText3).FirstOrDefault(),
                        //CalText4 = g.Select(x => x.AnalyticalLayer.CalText4).FirstOrDefault(),
                        CalText5 = g.Select(x => x.AnalyticalLayer.CalText).FirstOrDefault(),

                        FiveLevelInterpretations = g.First().AnalyticalLayer.FiveLevelInterpretations,

                        cities = g.Select(x => new MutipleCitieskpiLayerResults
                        {
                            CityID = x.CityID,
                            InterpretationID = x.InterpretationID,
                            NormalizeValue = x.NormalizeValue,
                            CalValue1 = x.CalValue1,
                            CalValue2 = x.CalValue2,
                            CalValue3 = x.CalValue3,
                            CalValue4 = x.CalValue4,
                            CalValue5 = x.CalValue5,
                            LastUpdated = x.LastUpdated,

                            AiInterpretationID = x.AiInterpretationID,
                            AiNormalizeValue = x.AiNormalizeValue,
                            AiCalValue1 = x.AiCalValue1,
                            AiCalValue2 = x.AiCalValue2,
                            AiCalValue3 = x.AiCalValue3,
                            AiCalValue4 = x.AiCalValue4,
                            AiCalValue5 = x.AiCalValue5,

                            AiLastUpdated = x.AiLastUpdated,
                            City = x.City
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                return ResultResponseDto<GetMutiplekpiLayerResultsDto>
                    .Success(response ?? new GetMutiplekpiLayerResultsDto());
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetMutiplekpiLayerResults", ex);

                return ResultResponseDto<GetMutiplekpiLayerResultsDto>
                    .Failure(new List<string> { "An error occurred." });
            }
        }

    }
}
