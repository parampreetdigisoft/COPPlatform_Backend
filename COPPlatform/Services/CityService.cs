using ClosedXML.Excel;
using COPPlatform.Common.Interface;
using COPPlatform.Common.Models;
using COPPlatform.Data;
using COPPlatform.Dtos.CityDto;
using COPPlatform.Dtos.dashboard;
using COPPlatform.IServices;
using COPPlatform.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;

namespace COPPlatform.Services
{
    public class CityService : ICityService
    {
        #region constructor

        private readonly ApplicationDbContext _context;
        private readonly IAppLogger _appLogger;
        private readonly IWebHostEnvironment _env;
        private readonly ICommonService _commonService;
        public CityService(ApplicationDbContext context, IAppLogger appLogger, IWebHostEnvironment env, ICommonService commonService)
        {
            _context = context;
            _appLogger = appLogger;
            _env = env;
            _commonService = commonService;
        }

        #endregion

        #region  methods Implementations

     

        #endregion
       

        public async Task<ResultResponseDto<object>> AssingCityToUser(int userId, int cityId, int assignedByUserId)
        {
            try
            {
                if (_context.UserAssessmentMappings.Any(x => x.UserID == userId && x.CityID == cityId && x.AssignedByUserId == assignedByUserId && !x.IsDeleted))
                {
                    return await Task.FromResult(ResultResponseDto<object>.Failure(new string[] { "City already assigned to user" }));
                }
                var user = _context.Users.Find(userId);

                if (user == null)
                {
                    return await Task.FromResult(ResultResponseDto<object>.Failure(new string[] { "Invalid request data." }));
                }
                var mapping = new UserAssessmentMapping
                {
                    UserID = userId,
                    CityID = cityId,
                    AssignedByUserId = assignedByUserId,
                    Role = user.Role
                };
                _context.UserAssessmentMappings.Add(mapping);

                await _context.SaveChangesAsync();

                return await Task.FromResult(ResultResponseDto<object>.Success(new { }, new string[] { "City assigned successfully" }));
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in AssingCityToUser", ex);
                return ResultResponseDto<object>.Failure(new string[] { "There is an error please try later" });
            }
        }

        public async Task<ResultResponseDto<object>> EditAssingCity(int id, int userId, int cityId, int assignedByUserId)
        {
            try
            {

                if (_context.UserAssessmentMappings.Any(x => x.UserID == userId && x.CityID == cityId && x.AssignedByUserId == assignedByUserId))
                {
                    return ResultResponseDto<object>.Failure(new string[] { "City already assigned to user" });
                }
                var userMapping = _context.UserAssessmentMappings.Find(id);

                if (userMapping == null)
                {
                    return ResultResponseDto<object>.Failure(new string[] { "Invalid request data." });
                }

                userMapping.UserID = userId;
                userMapping.CityID = cityId;
                userMapping.AssignedByUserId = assignedByUserId;
                _context.UserAssessmentMappings.Update(userMapping);
                await _context.SaveChangesAsync();

                return ResultResponseDto<object>.Success(new { }, new string[] { "Assigned city updated successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure", ex);
                return ResultResponseDto<object>.Failure(new string[] { "There is an error please try later" });
            }
        }

        public async Task<ResultResponseDto<object>> UnAssignCity(UserCityUnMappingRequestDto requestDto)
        {
            try
            {
                var userMapping = _context.UserAssessmentMappings.Where(x => x.UserID == requestDto.UserId && x.AssignedByUserId == requestDto.AssignedByUserId && !x.IsDeleted).ToList();
                if (userMapping == null && userMapping?.Count == 0)
                {
                    return await Task.FromResult(ResultResponseDto<object>.Failure(new string[] { "user has no assign city" }));
                }
                foreach (var m in userMapping)
                {
                    m.IsDeleted = true;
                    _context.UserAssessmentMappings.Update(m);
                }

                await _context.SaveChangesAsync();

                return ResultResponseDto<object>.Success(new { }, new string[] { "Assigned city deleted successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in UnAssignCity", ex);
                return ResultResponseDto<object>.Failure(new string[] { "There is an error please try later" });
            }
        }

        public async Task<ResultResponseDto<byte[]>> ExportCities(int userId, UserRole userRole)
        {
            try
            {
                int year = DateTime.UtcNow.Year;
                var cities = await _commonService.GetCitiesProgressForAdmin(userId, (int)userRole, year);

                if (cities == null) return ResultResponseDto<byte[]>.Failure(new string[] { "There is an error please try later" });
                IEnumerable<IGrouping<(int CityID, string CityName, string State, string Country), GetCitiesProgressAdminDto>>
                result =
                    cities.GroupBy(x => (
                        x.CityID,
                        x.CityName,
                        x.State,
                        x.Country
                    ));
                var byteRes = MakeCityPillarSheet(result);

                return ResultResponseDto<byte[]>.Success(byteRes, new string[] { "get successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in getAllCityByUserId", ex);
                return ResultResponseDto<byte[]>.Failure(new string[] { "There is an error please try later" });
            }
        }

        private byte[] MakeCityPillarSheet(IEnumerable<IGrouping<(int CityID, string CityName, string State, string Country), GetCitiesProgressAdminDto>> cityGroups)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Cities Progress Report");

                // ----------------------------
                // Header Section
                // ----------------------------
                ws.Range("A1:J1").Merge().Value = "Cities Progress Report";
                ws.Range("A2:J2").Merge().Value = $"Report Year: {DateTime.UtcNow.Year}";
                ws.Range("A3:J3").Merge().Value = $"Generated On: {DateTime.UtcNow:dd-MMM-yyyy HH:mm}";

                var headerRange = ws.Range("A1:J3");
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(57, 123, 103);
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontSize = 14;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                int row = 5;

                // ----------------------------
                // Column Headers
                // ----------------------------
                ws.Cell(row, 1).Value = "S.No.";
                ws.Cell(row, 2).Value = "City Name";
                ws.Cell(row, 3).Value = "State";
                ws.Cell(row, 4).Value = "Country";
                ws.Cell(row, 5).Value = "Pillar Name";
                ws.Cell(row, 6).Value = "Total Score";
                ws.Cell(row, 7).Value = "Total Answers";
                ws.Cell(row, 8).Value = "Evaluator Pillar Progress (%)";
                ws.Cell(row, 9).Value = "AI Pillar Progress (%)";
                ws.Cell(row, 10).Value = "Evaluator - AI City Progress (%)";

                var columnHeader = ws.Range(row, 1, row, 10);
                columnHeader.Style.Font.Bold = true;
                columnHeader.Style.Fill.BackgroundColor = XLColor.FromArgb(57, 123, 103);
                columnHeader.Style.Font.FontColor = XLColor.White;
                columnHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                columnHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                columnHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                columnHeader.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws.Row(row).Height = 25;

                row++;
                int sno = 1;

                // ----------------------------
                // Data Rows
                // ----------------------------
                foreach (var cityGroup in cityGroups)
                {
                    var cityData = cityGroup.First();
                    var pillars = cityGroup.OrderBy(x => x.DisplayOrder).ToList();

                    int startRow = row;
                    bool isFirstPillar = true;

                    var cityProgress = pillars.Average(x => x.PillarProgress);
                    foreach (var pillar in pillars)
                    {
                        ws.Cell(row, 1).Value = sno++;
                        ws.Cell(row, 2).Value = cityData.CityName;
                        ws.Cell(row, 3).Value = cityData.State;
                        ws.Cell(row, 4).Value = cityData.Country;
                        ws.Cell(row, 5).Value = pillar.PillarName;
                        ws.Cell(row, 6).Value = pillar.TotalScore;
                        ws.Cell(row, 7).Value = pillar.TotalAns;
                        ws.Cell(row, 8).Value = $"{pillar.PillarProgress:F2}%";
                        ws.Cell(row, 9).Value = $"{pillar.AIPillarProgress:F2}%";

                        // City progress only in first row for each city
                        if (isFirstPillar)
                        {
                            ws.Cell(row, 10).Value = $"{cityProgress:F2}% - {cityData.AICityProgress:F2}%";
                            ws.Cell(row, 10).Style.Font.Bold = true;
                            ws.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.FromArgb(57, 123, 103);
                            isFirstPillar = false;
                        }

                        // Style data rows
                        var dataRow = ws.Range(row, 1, row, 10);
                        dataRow.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        dataRow.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRow.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        dataRow.Style.Border.DiagonalBorderColor = XLColor.LightGray;

                        // Center align S.No and numeric columns
                        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Bold pillar names
                        ws.Cell(row, 5).Style.Font.FontColor = XLColor.FromArgb(23, 55, 94);

                        row++;
                    }

                    // Merge city information cells for better visual grouping
                    int endRow = row - 1;
                    if (endRow > startRow)
                    {
                        ws.Range(startRow, 2, endRow, 2).Merge(); // City Name
                        ws.Range(startRow, 3, endRow, 3).Merge(); // State
                        ws.Range(startRow, 4, endRow, 4).Merge(); // Country
                        ws.Range(startRow, 10, endRow, 10).Merge(); // City Progress
                    }

                    // Add visual separator between cities
                    var cityRange = ws.Range(startRow, 1, endRow, 10);
                    cityRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                    cityRange.Style.Border.OutsideBorderColor = XLColor.FromArgb(57, 123, 103);

                    // Highlight city name cells
                    ws.Range(startRow, 2, endRow, 2).Style.Font.Bold = true;
                    ws.Range(startRow, 2, endRow, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(57, 123, 103);

                    // Alternate city group background
                    if ((cityGroups.ToList().IndexOf(cityGroup) + 1) % 2 == 0)
                    {
                        cityRange.Style.Fill.BackgroundColor = XLColor.FromArgb(250, 250, 250);
                    }
                }

                ws.Column(1).Width = 8;
                ws.Column(2).Width = 20;
                ws.Column(3).Width = 15;
                ws.Column(4).Width = 15;
                ws.Column(5).Width = 45;
                ws.Column(6).Width = 14;
                ws.Column(7).Width = 14;
                ws.Column(8).Width = 25;
                ws.Column(9).Width = 20;
                ws.Column(10).Width = 28;


                ws.SheetView.FreezeRows(5);
                ws.SheetView.FreezeColumns(1);

                var usedRange = ws.RangeUsed();
                if (usedRange != null)
                {
                    ws.Range(5, 1, 5, 10).SetAutoFilter();
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public async Task<ResultResponseDto<CardDetailsDto>> GetCardDetails(int userID, UserRole userRole)
        {
            try
            {
                var result = new CardDetailsDto();

                Expression<Func<UserAssessmentMapping, bool>> predicate;

                if (userRole == UserRole.Analyst)
                    predicate = x => !x.IsDeleted && x.IsActive && x.UserID == userID;
                else if (userRole == UserRole.Evaluator)
                    predicate = x => !x.IsDeleted && x.IsActive &&
                                     x.UserPillarMappings.Any(up => up.UserID == userID);
                else if (userRole == UserRole.Admin)
                    predicate = x => !x.IsDeleted && x.IsActive && x.AssignedByUserId == userID;
                else
                    predicate = x => !x.IsDeleted;

                // STEP 1: Fetch data (safe LEFT JOIN)
                var data = await (
                    from uc in _context.UserAssessmentMappings.Where(predicate)
                    join a in _context.Assessments.Where(x => x.IsActive)
                        on uc.UserAssessmentMappingID equals a.UserAssessmentMappingID into gj
                    from a in gj.DefaultIfEmpty()
                    select new
                    {
                        uc.UserAssessmentMappingID,

                        PillarAssessments = a.PillarAssessments
                            .Where(pa => !pa.IsDeleted)
                            .Select(pa => new
                            {
                                pa.PillarID,
                                Responses = pa.Responses
                                    .Where(r => !r.IsDeleted)
                                    .Select(r => r.Score)
                            })
                    }
                ).ToListAsync();

                // STEP 2: Total Questions
                var pillars = await _context.Pillars
                    .Select(p => new
                    {
                        p.PillarID,
                        p.PillarName,
                        Questions = p.Questions.Where(x => !x.IsDeleted).Count()
                    })
                    .ToListAsync();

                var totalQuestions = pillars.Select(x => x.Questions).Sum();


                var assessmentScores = new List<decimal>();
                var pillarScores = new List<PillarCardDetailsDto>();

                // STEP 3: Calculate scores
                foreach (var item in data)
                {
                    var allResponses = item.PillarAssessments
                        .SelectMany(p => p.Responses)
                        .Select(x => ((int?)x) ?? 0)
                        .ToList();

                    var totalAnswers = allResponses.Count;
                    var totalScore = allResponses.Sum();

                    if (totalAnswers > 0)
                    {
                        var assessmentScore = (totalScore * 100m) / (totalAnswers * 4m);
                        assessmentScores.Add(assessmentScore);
                    }

                    // Pillar-level score
                    foreach (var pillar in item.PillarAssessments)
                    {
                        var pResponses = pillar.Responses.ToList();
                        var pTotalAnswers = pResponses.Count;
                        var pTotalScore = pResponses.Sum(x => (int?)x ?? 0);

                        if (pTotalAnswers > 0)
                        {
                            var pScore = (pTotalScore * 100m) / (pTotalAnswers * 4m);

                            var p = new PillarCardDetailsDto
                            {
                                PillarID = pillar.PillarID,
                                Value = Math.Round(pScore, 2),
                            };
                            pillarScores.Add(p);
                        }
                    }
                }

                // STEP 4: Assessment counts
                result.TotalAssessments = data.Count;

                result.TotalCompletedAssessments = data.Count(x =>
                    x.PillarAssessments.SelectMany(p => p.Responses).Count() == totalQuestions);

                result.TotalInProgressAssessments =
                    result.TotalAssessments - result.TotalCompletedAssessments;

                // STEP 5: Score Aggregation
                if (pillarScores.Any())
                {
                    var pillarIdToName = pillars.ToDictionary(p => p.PillarID, p => p.PillarName);


                    result.AveragePillarScore = pillarScores.Any() ? pillarScores.Average(x => x.Value) : 0;

                    var maxPillar = pillarScores.OrderByDescending(x => x.Value).First();
                    result.HighestPillarScore = new PillarCardDetailsDto
                    {
                        PillarID = maxPillar.PillarID,
                        PillarName = pillarIdToName.ContainsKey(maxPillar.PillarID) ? pillarIdToName[maxPillar.PillarID] : "Unknown",
                        Value = maxPillar.Value
                    };

                    var minPillar = pillarScores.OrderBy(x => x.Value).First();
                    result.LowestPillarScore = new PillarCardDetailsDto
                    {
                        PillarID = minPillar.PillarID,
                        PillarName = pillarIdToName.ContainsKey(minPillar.PillarID) ? pillarIdToName[minPillar.PillarID] : "Unknown",
                        Value = minPillar.Value
                    };
                }


                // STEP 6: User counts (Admin only)
                if (userRole == UserRole.Admin || userRole== UserRole.Executive)
                {
                    var userCounts = await _context.Users
                        .Where(u => !u.IsDeleted)
                        .GroupBy(u => u.Role)
                        .Select(g => new { Role = g.Key, Count = g.Count() })
                        .ToListAsync();

                    result.TotalExecutives = userCounts.FirstOrDefault(x => x.Role == UserRole.Executive)?.Count ?? 0;
                    result.TotalAnalysts = userCounts.FirstOrDefault(x => x.Role == UserRole.Analyst)?.Count ?? 0;
                    result.TotalEvaluators = userCounts.FirstOrDefault(x => x.Role == UserRole.Evaluator)?.Count ?? 0;
                }
                else if (userRole == UserRole.Analyst)
                {
                    var evaluatorCount = await _context.Users
                        .Where(u => !u.IsDeleted &&
                                    u.Role == UserRole.Evaluator &&
                                    u.CreatedBy == userID)
                        .CountAsync();

                    result.TotalEvaluators = evaluatorCount;
                }
                // 1. Base mappings
                var mappings = await _context.UserAssessmentMappings
                    .Where(predicate)
                    .Select(x => new
                    {
                        x.UserAssessmentMappingID,
                        x.DueDate,
                        x.UpdatedAt
                    })
                    .ToListAsync();

                var mappingIds = mappings.Select(x => x.UserAssessmentMappingID).ToList();

                // 2. Pillars per mapping
                var pillarData = await _context.Assessments
                    .Where(a => a.IsActive && mappingIds.Contains(a.UserAssessmentMappingID))
                    .SelectMany(a => a.PillarAssessments)
                    .Where(pa => !pa.IsDeleted)
                    .GroupBy(pa => pa.Assessment.UserAssessmentMappingID)
                    .Select(g => new
                    {
                        MappingID = g.Key,
                        Pillars = g.Select(x => x.PillarID).Distinct().ToList()
                    })
                    .ToListAsync();

                var pillarDict = pillarData.ToDictionary(x => x.MappingID, x => x.Pillars);

                // 3. Answered Questions
                var answeredData = await _context.Assessments
                    .Where(a => a.IsActive && mappingIds.Contains(a.UserAssessmentMappingID))
                    .SelectMany(a => a.PillarAssessments)
                    .SelectMany(pa => pa.Responses)
                    .Where(r => !r.IsDeleted)
                    .GroupBy(r => r.PillarAssessment.Assessment.UserAssessmentMappingID)
                    .Select(g => new
                    {
                        MappingID = g.Key,
                        Count = g.Select(x => x.QuestionID).Distinct().Count()
                    })
                    .ToListAsync();

                var answeredDict = answeredData.ToDictionary(x => x.MappingID, x => x.Count);

                // 4. Question count per pillar
                var questionCounts = await _context.Questions
                    .Where(q => !q.IsDeleted)
                    .GroupBy(q => q.PillarID)
                    .Select(g => new { PillarID = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.PillarID, x => x.Count);

                int overdue = 0, highRisk = 0, atRisk = 0, dueSoon = 0, onTrack = 0;

                foreach (var m in mappings)
                {
                    if (!m.DueDate.HasValue)
                    {
                        onTrack++;
                        continue;
                    }

                    var pillarsList = pillarDict.ContainsKey(m.UserAssessmentMappingID)
                        ? pillarDict[m.UserAssessmentMappingID]
                        : new List<int>();

                    var totalQ = pillarsList.Sum(p => questionCounts.ContainsKey(p) ? questionCounts[p] : 0);

                    var answered = answeredDict.ContainsKey(m.UserAssessmentMappingID)
                        ? answeredDict[m.UserAssessmentMappingID]
                        : 0;

                    var totalDays = (m.DueDate - m.UpdatedAt)?.Days ?? 0;
                    var daysElapsed = (DateTime.UtcNow - m.UpdatedAt)?.Days ?? 0;
                    var daysRemaining = (m.DueDate - DateTime.UtcNow)?.Days ?? 0;

                    var progress = totalQ == 0 ? 0 : (answered * 100m) / totalQ;
                    var expected = totalDays == 0 ? 0 : (daysElapsed * 100m) / totalDays;

                    if (DateTime.UtcNow > m.DueDate)
                        overdue++;
                    else if (progress < expected && daysRemaining <= 3)
                        highRisk++;
                    else if (progress < expected)
                        atRisk++;
                    else if (daysRemaining <= 3)
                        dueSoon++;
                    else
                        onTrack++;
                }

                result.TotalOverdue = overdue;
                result.TotalHighRisk = highRisk;
                result.TotalAtRisk = atRisk;
                result.TotalDueSoon = dueSoon;
                result.TotalOnTrack = onTrack;
                if (userRole == UserRole.Analyst)
                {
                    var history = await _commonService
                        .GetUserProgressByAssessmentId(null);

                    var userIds = await _context.Users
                        .Where(u => !u.IsDeleted && u.CreatedBy == userID)
                        .Select(u => u.UserID)
                        .ToListAsync();

                    var userPillars = await _context.UserPillarMappings
                        .Where(x => x.UserID == userID && !x.IsDeleted && x.IsActive)
                        .Select(x => x.PillarID)
                        .ToListAsync();

                    // 🔥 Use HashSet for better performance
                    var userIdSet = userIds.ToHashSet();
                    var pillarIdSet = userPillars.ToHashSet();

                    var filteredHistory = history
                        .Where(x => pillarIdSet.Contains(x.PillarID) &&
                                    userIdSet.Contains(x.SubmittedByUserID))
                        .ToList();

                    var evaluatorSummary = filteredHistory
                                    .GroupBy(u => u.SubmittedByUserID)
                                    .Select(g =>
                                    {
                                        var first = g.First();
                                        return new EvaluatorCompletionSummaryDto
                                        {
                                            EvaluatorName = first.SubmittedByUserName ?? "",
                                            CompletionRate = first.CompletionRate
                                        };
                                    })
                                    .ToList();
                    var maxEvaluator = evaluatorSummary
    .OrderByDescending(x => x.CompletionRate)
    .FirstOrDefault();

                    var minEvaluator = evaluatorSummary
                        .OrderBy(x => x.CompletionRate)
                        .FirstOrDefault();
                    result.MaximumCompletionRateEvaluator = maxEvaluator;
                    result.MinimumCompletionRateEvaluator = minEvaluator;
                }



                return ResultResponseDto<CardDetailsDto>.Success(result,
                        new List<string> { "Card details fetched successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetCardDetails", ex);
                return ResultResponseDto<CardDetailsDto>.Failure(
                    new[] { "There was an error. Please try again later." });
            }
        }


        public async Task<ResultResponseDto<CardDetailsDto>> GetExecutiveCardDetails(int userID, UserRole userRole)
        {
            try
            {
                Expression<Func<UserAssessmentMapping, bool>> predicate = x => !x.IsDeleted;

                var data = await LoadAssessmentScoreDataAsync(predicate);
                var pillars = await LoadPillarQuestionCountsAsync();
                var (totalCritical, answeredCritical) = await GetCriticalQuestionStatsAsync();
                var riskMetrics = await ComputeExecutiveRiskMetricsAsync(predicate);

                var result = new CardDetailsDto();
                await ApplyExecutiveUserCountsAsync(result, userID, userRole);

                var totalQuestions = pillars.Sum(p => p.QuestionCount);

                ApplyAssessmentCounts(result, data, totalQuestions);
                ApplyPillarScoreSummary(result, CalculatePillarScores(data, pillars), pillars);
                ApplyRiskMetrics(result, riskMetrics);

                result.TotalCriticalQuestions = totalCritical;
                result.TotalAnsweredCriticalQuestions = answeredCritical;

                return ResultResponseDto<CardDetailsDto>.Success(
                    result,
                    new List<string> { "Card details fetched successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetExecutiveCardDetails", ex);
                return ResultResponseDto<CardDetailsDto>.Failure(
                    new[] { "There was an error. Please try again later." });
            }
        }

        #region GetCitiesAsync

        #region Executive card details helpers

        private async Task<List<AssessmentScoreRow>> LoadAssessmentScoreDataAsync(
            Expression<Func<UserAssessmentMapping, bool>> predicate)
        {
            return await (
                from uc in _context.UserAssessmentMappings.Where(predicate)
                join a in _context.Assessments.Where(x => x.IsActive)
                    on uc.UserAssessmentMappingID equals a.UserAssessmentMappingID into gj
                from a in gj.DefaultIfEmpty()
                select new AssessmentScoreRow
                {
                    UserAssessmentMappingID = uc.UserAssessmentMappingID,
                    PillarAssessments = a.PillarAssessments
                        .Where(pa => !pa.IsDeleted)
                        .Select(pa => new PillarAssessmentScoreRow
                        {
                            PillarID = pa.PillarID,
                            Responses = pa.Responses
                                .Where(r => !r.IsDeleted)
                                .Select(r => r.Score)
                        })
                }
            ).ToListAsync();
        }

        private async Task<List<PillarQuestionCountRow>> LoadPillarQuestionCountsAsync()
        {
            return await _context.Pillars
                .Select(p => new PillarQuestionCountRow
                {
                    PillarID = p.PillarID,
                    PillarName = p.PillarName,
                    QuestionCount = p.Questions.Count(x => !x.IsDeleted)
                })
                .ToListAsync();
        }

        private async Task<(int Total, int Answered)> GetCriticalQuestionStatsAsync()
        {
            var total = await _context.Questions
                .CountAsync(q => !q.IsDeleted && q.IsCritical);

            var answered = await (
                from r in _context.AssessmentResponses
                join pa in _context.PillarAssessments on r.PillarAssessmentID equals pa.PillarAssessmentID
                join q in _context.Questions on r.QuestionID equals q.QuestionID
                join a in _context.Assessments on pa.AssessmentID equals a.AssessmentID
                join m in _context.UserAssessmentMappings on a.UserAssessmentMappingID equals m.UserAssessmentMappingID
                where !r.IsDeleted && !pa.IsDeleted && !m.IsDeleted && q.IsCritical
                select r.QuestionID
            ).CountAsync();

            return (total, answered);
        }

        private static void ApplyAssessmentCounts(
            CardDetailsDto result,
            IReadOnlyList<AssessmentScoreRow> data,
            int totalQuestions)
        {
            result.TotalAssessments = data.Count;
            result.TotalCompletedAssessments = data.Count(x =>
                x.PillarAssessments.SelectMany(p => p.Responses).Count() == totalQuestions);
            result.TotalInProgressAssessments =
                result.TotalAssessments - result.TotalCompletedAssessments;
        }

        private static List<PillarCardDetailsDto> CalculatePillarScores(IEnumerable<AssessmentScoreRow> data, List<PillarQuestionCountRow> pillars)
        {
            var pillarScores = new List<PillarCardDetailsDto>();

            foreach (var item in data)
            {
                foreach (var pillar in item.PillarAssessments)
                {
                    var scores = pillar.Responses.Select(x => x.HasValue ? (int)x.Value : 0).ToList();
                    if (scores.Count == 0)
                        continue;

                    var questionCount = pillars.FirstOrDefault(x => x.PillarID == pillar.PillarID)?.QuestionCount ?? 1;

                    var pScore = (scores.Sum() * 100m) / (questionCount * 4m);
                    pillarScores.Add(new PillarCardDetailsDto
                    {
                        PillarID = pillar.PillarID,
                        Value = Math.Round(pScore, 2)
                    });
                }
            }

            return pillarScores;
        }

        private static void ApplyPillarScoreSummary(
            CardDetailsDto result,
            List<PillarCardDetailsDto> pillarScores,
            List<PillarQuestionCountRow> pillars)
        {
            if (pillarScores.Count == 0)
                return;

            var pillarIdToName = pillars.ToDictionary(p => p.PillarID, p => p.PillarName);
            result.AveragePillarScore = pillarScores.Average(x => x.Value);

            var maxPillar = pillarScores.MaxBy(x => x.Value)!;
            var minPillar = pillarScores.MinBy(x => x.Value)!;

            result.HighestPillarScore = ToPillarCardDetails(maxPillar, pillarIdToName);
            result.LowestPillarScore = ToPillarCardDetails(minPillar, pillarIdToName);
        }

        private static PillarCardDetailsDto ToPillarCardDetails(
            PillarCardDetailsDto pillar,
            Dictionary<int, string> pillarIdToName) =>
            new()
            {
                PillarID = pillar.PillarID,
                PillarName = pillarIdToName.GetValueOrDefault(pillar.PillarID, "Unknown"),
                Value = pillar.Value
            };

        private async Task ApplyExecutiveUserCountsAsync(CardDetailsDto result, int userID, UserRole userRole)
        {
            if (userRole is UserRole.Admin or UserRole.Executive)
            {
                var userCounts = await _context.Users
                    .Where(u => !u.IsDeleted)
                    .GroupBy(u => u.Role)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToListAsync();

                result.TotalExecutives = userCounts.FirstOrDefault(x => x.Role == UserRole.Executive)?.Count ?? 0;
                result.TotalAnalysts = userCounts.FirstOrDefault(x => x.Role == UserRole.Analyst)?.Count ?? 0;
                result.TotalEvaluators = userCounts.FirstOrDefault(x => x.Role == UserRole.Evaluator)?.Count ?? 0;
            }
            else if (userRole == UserRole.Analyst)
            {
                result.TotalEvaluators = await _context.Users
                    .Where(u => !u.IsDeleted && u.Role == UserRole.Evaluator && u.CreatedBy == userID)
                    .CountAsync();
            }
        }

        private async Task<ExecutiveRiskMetrics> ComputeExecutiveRiskMetricsAsync(
            Expression<Func<UserAssessmentMapping, bool>> predicate)
        {
            var mappings = await _context.UserAssessmentMappings
                .Where(predicate)
                .Select(x => new RiskMappingRow
                {
                    UserAssessmentMappingID = x.UserAssessmentMappingID,
                    DueDate = x.DueDate,
                    UpdatedAt = x.UpdatedAt,
                    AssessmentName = x.GeographicReference,
                    OwnerName = x.User.FullName
                })
                .ToListAsync();

            var mappingIds = mappings.Select(x => x.UserAssessmentMappingID).ToList();
            if (mappingIds.Count == 0)
                return new ExecutiveRiskMetrics();

            var pillarData = await _context.Assessments
                .Where(a => a.IsActive && mappingIds.Contains(a.UserAssessmentMappingID))
                .SelectMany(a => a.PillarAssessments)
                .Where(pa => !pa.IsDeleted)
                .GroupBy(pa => pa.Assessment.UserAssessmentMappingID)
                .Select(g => new { MappingID = g.Key, Pillars = g.Select(x => x.PillarID).Distinct().ToList() })
                .ToListAsync();

            var answeredData = await _context.Assessments
                .Where(a => a.IsActive && mappingIds.Contains(a.UserAssessmentMappingID))
                .SelectMany(a => a.PillarAssessments)
                .SelectMany(pa => pa.Responses)
                .Where(r => !r.IsDeleted)
                .GroupBy(r => r.PillarAssessment.Assessment.UserAssessmentMappingID)
                .Select(g => new { MappingID = g.Key, Count = g.Select(x => x.QuestionID).Distinct().Count() })
                .ToListAsync();

            var questionCounts = await _context.Questions
                .Where(q => !q.IsDeleted)
                .GroupBy(q => q.PillarID)
                .Select(g => new { PillarID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PillarID, x => x.Count);

            var pillarDict = pillarData.ToDictionary(x => x.MappingID, x => x.Pillars);
            var answeredDict = answeredData.ToDictionary(x => x.MappingID, x => x.Count);

            return BuildExecutiveRiskMetrics(mappings, pillarDict, answeredDict, questionCounts);
        }

        private static ExecutiveRiskMetrics BuildExecutiveRiskMetrics(
            List<RiskMappingRow> mappings,
            Dictionary<int, List<int>> pillarDict,
            Dictionary<int, int> answeredDict,
            Dictionary<int, int> questionCounts)
        {
            var metrics = new ExecutiveRiskMetrics();
            var utcNow = DateTime.UtcNow;

            foreach (var m in mappings)
            {
                if (!m.DueDate.HasValue)
                {
                    metrics.OnTrack++;
                    metrics.Details.Add(new RiskDetailDto
                    {
                        MappingId = m.UserAssessmentMappingID,
                        AssessmentName = m.AssessmentName,
                        OwnerName = m.OwnerName,
                        DueDate = null,
                        Progress = 0,
                        RiskLevel = "On Track",
                        DaysRemaining = 0
                    });
                    continue;
                }

                var pillarsList = pillarDict.GetValueOrDefault(m.UserAssessmentMappingID, new List<int>());
                var totalQ = pillarsList.Sum(p => questionCounts.GetValueOrDefault(p, 0));
                var answered = answeredDict.GetValueOrDefault(m.UserAssessmentMappingID, 0);

                var totalDays = (m.DueDate - m.UpdatedAt)?.Days ?? 0;
                var daysElapsed = (utcNow - m.UpdatedAt)?.Days ?? 0;
                var daysRemaining = (m.DueDate - utcNow)?.Days ?? 0;

                var progress = totalQ == 0 ? 0 : (answered * 100m) / totalQ;
                var expected = totalDays == 0 ? 0 : (daysElapsed * 100m) / totalDays;
                var riskLevel = ClassifyRiskLevel(utcNow, m.DueDate, progress, expected, daysRemaining);

                IncrementRiskBucket(metrics, riskLevel);
                metrics.Details.Add(new RiskDetailDto
                {
                    MappingId = m.UserAssessmentMappingID,
                    AssessmentName = m.AssessmentName,
                    OwnerName = m.OwnerName,
                    DueDate = m.DueDate,
                    Progress = Math.Round(progress, 2),
                    RiskLevel = riskLevel,
                    DaysRemaining = daysRemaining
                });
            }

            metrics.Details = metrics.Details
                .OrderBy(r => RiskLevelSortKey(r.RiskLevel))
                .ThenBy(r => r.DaysRemaining)
                .ToList();

            return metrics;
        }

        private static string ClassifyRiskLevel(
            DateTime utcNow,
            DateTime? dueDate,
            decimal progress,
            decimal expected,
            int daysRemaining)
        {
            if (utcNow > dueDate)
                return "Overdue";
            if (progress < expected && daysRemaining <= 3)
                return "High Risk";
            if (progress < expected)
                return "At Risk";
            if (daysRemaining <= 3)
                return "Due Soon";
            return "On Track";
        }

        private static void IncrementRiskBucket(ExecutiveRiskMetrics metrics, string riskLevel)
        {
            switch (riskLevel)
            {
                case "Overdue": metrics.Overdue++; break;
                case "High Risk": metrics.HighRisk++; break;
                case "At Risk": metrics.AtRisk++; break;
                case "Due Soon": metrics.DueSoon++; break;
                default: metrics.OnTrack++; break;
            }
        }

        private static int RiskLevelSortKey(string riskLevel) => riskLevel switch
        {
            "Overdue" => 1,
            "High Risk" => 2,
            "At Risk" => 3,
            "Due Soon" => 4,
            _ => 5
        };

        private static void ApplyRiskMetrics(CardDetailsDto result, ExecutiveRiskMetrics metrics)
        {
            result.TotalOverdue = metrics.Overdue;
            result.TotalHighRisk = metrics.HighRisk;
            result.TotalAtRisk = metrics.AtRisk;
            result.TotalDueSoon = metrics.DueSoon;
            result.TotalOnTrack = metrics.OnTrack;
            result.RiskDetails = metrics.Details;
        }

        #endregion

        #endregion
    }
}
