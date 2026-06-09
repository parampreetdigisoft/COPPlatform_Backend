using ClosedXML.Excel;
using COPPlatform.Backgroundjob;
using COPPlatform.Common.Implementation;
using COPPlatform.Common.Interface;
using COPPlatform.Common.Models;
using COPPlatform.Data;
using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.CityDto;
using COPPlatform.Dtos.CommonDto;
using COPPlatform.Dtos.dashboard;
using COPPlatform.IServices;
using COPPlatform.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace COPPlatform.Services
{
    public class AssessmentResponseService : IAssessmentResponseService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAppLogger _appLogger;
        private readonly Download _download;
        private readonly ICommonService _commonService;
        public AssessmentResponseService(ApplicationDbContext context, IAppLogger appLogger, Download download, ICommonService commonService)
        {
            _context = context;
            _appLogger = appLogger;
            _download = download;
            _commonService = commonService;
        }

        public async Task<List<AssessmentResponse>> GetAllAsync()
        {
            try
            {
                return await _context.AssessmentResponses.ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in GetAllAsync", ex);
                return new List<AssessmentResponse>();
            }
        }
        public async Task<AssessmentResponse?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.AssessmentResponses.FindAsync(id);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in GetByIdAsync ", ex);
                return new AssessmentResponse();
            }

        }
        public async Task<AssessmentResponse> AddAsync(AssessmentResponse response)
        {
            try
            {
                _context.AssessmentResponses.Add(response);
                await _context.SaveChangesAsync();
                return response;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in AddAsync", ex);
                return new AssessmentResponse();
            }
        }
        public async Task<AssessmentResponse?> UpdateAsync(int id, AssessmentResponse response)
        {
            try
            {
                var existing = await _context.AssessmentResponses.FindAsync(id);
                if (existing == null) return null;
                existing.Score = response.Score;
                existing.Justification = response.Justification;
                await _context.SaveChangesAsync();
                return existing;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in UpdateAsync", ex);
                return new AssessmentResponse();
            }

        }
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var resp = await _context.AssessmentResponses.FindAsync(id);
                if (resp == null) return false;
                _context.AssessmentResponses.Remove(resp);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure DeleteAsync", ex);
                return false;
            }

        }
        public async Task<ResultResponseDto<string>> SaveAssessment(AddAssessmentDto request, int userID, UserRole userRole)
        {
            try
            {
                var now = DateTime.UtcNow;
                var assessment = await _context.Assessments
                    .Include(x=>x.UserAssessmentMapping)
                    .Include(x => x.PillarAssessments)
                    .ThenInclude(x => x.Responses)
                     .ThenInclude(r => r.AssessmentResponseHistories)
                    .FirstOrDefaultAsync(x =>
                        x.IsActive  &&
                        (x.AssessmentID == request.AssessmentID || x.UserAssessmentMappingID == request.UserAssessmentMappingID));

                // If no assessment found, create a new one
                if (assessment == null)
                {
                    var ucm = await _context.UserAssessmentMappings
                        .FirstOrDefaultAsync(x => x.UserAssessmentMappingID == request.UserAssessmentMappingID);

                    if (ucm == null)
                        return ResultResponseDto<string>.Failure(new[] { "invitation is not assigned" });

                    assessment = new Assessment
                    {
                        UserAssessmentMappingID = ucm.UserAssessmentMappingID,
                        CreatedAt = now,
                        UpdatedAt = now,
                        IsActive = true,
                        UserAssessmentMapping = ucm,
                        AssessmentPhase = AssessmentPhase.InProgress
                    };
                    _context.Assessments.Add(assessment);
                }

                if (request.PillarID > 0)
                {
                    var pillarAssessment = assessment.PillarAssessments
                        .FirstOrDefault(x => x.PillarID == request.PillarID);

                    if (pillarAssessment == null)
                    {
                        // Create new pillar assessment
                        pillarAssessment = new PillarAssessment
                        {
                            PillarID = request.PillarID,
                            Assessment = assessment
                        };
                        assessment.PillarAssessments.Add(pillarAssessment);
                    }

                    var existingResponses = pillarAssessment.Responses.ToList();


                    // ADD or UPDATE responses
                    foreach (var response in request.Responses)
                    {
                        var existing = existingResponses
                            .FirstOrDefault(r => r.ResponseID == response.ResponseID || r.QuestionID == response.QuestionID);

                        if (existing == null && !string.IsNullOrEmpty(response.Justification))
                        {
                            // Add new
                            pillarAssessment.Responses.Add(new AssessmentResponse
                            {
                                QuestionID = response.QuestionID,
                                QuestionOptionID = response.QuestionOptionID,
                                Justification = response.Justification,
                                Source = response.Source,
                                Score = response.Score,
                                UpdatedBy = userID,
                                UpdatedAt = now,
                                AssessmentResponseHistories = new List<AssessmentResponseHistory>
                                {
                                    new AssessmentResponseHistory
                                    {
                                        Justification = response.Justification,
                                        Score = response.Score,
                                        Source = response.Source,
                                        QuestionID = response.QuestionID,
                                        QuestionOptionID = response.QuestionOptionID,
                                        UpdatedAt = now,
                                        UserID = userID,
                                        IsDeleted= false
                                    }
                                }
                            });
                        }
                        else
                        {
                            // Update existing
                            existing.QuestionID = response.QuestionID;
                            existing.QuestionOptionID = response.QuestionOptionID;
                            existing.Justification = response.Justification;
                            existing.Score = response.Score;
                            existing.Source = response.Source;
                            existing.UpdatedAt  = now;
                            existing.UpdatedBy = userID;

                            var history = existing.AssessmentResponseHistories?.FirstOrDefault(h => h.UserID == userID);

                            if (history == null)
                            {
                                existing.AssessmentResponseHistories.Add(new AssessmentResponseHistory
                                {
                                    Justification = response.Justification,
                                    Score = response.Score,
                                    Source = response.Source,
                                    UpdatedAt = now,
                                    UserID = userID,
                                    QuestionID = response.QuestionID,
                                    QuestionOptionID = response.QuestionOptionID,
                                    IsDeleted = false
                                });
                            }
                            else
                            {
                                history.Justification = response.Justification;
                                history.Score = response.Score;
                                history.Source = response.Source;
                                history.UpdatedAt = now;
                                history.QuestionID = response.QuestionID;
                                history.QuestionOptionID = response.QuestionOptionID;
                            }
                        }
                    }
                    if (request.IsFinalized)
                    {
                        assessment.AssessmentPhase = AssessmentPhase.Completed;
                    }

                    assessment.UpdatedAt = now;
                }

                await _context.SaveChangesAsync();

                //_download.InsertAnalyticalLayerResults(assessment.UserAssessmentMapping.Year);

                return ResultResponseDto<string>.Success("", new[] { "Pillar saved successfully" }, 1);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occurred in SaveAssessment", ex);
                return ResultResponseDto<string>.Failure(new[] { "Failed to save assessment" });
            }
        }

        public async Task<PaginationResponse<GetAssessmentResponseDto>> GetAssessmentResult(GetAssessmentRequestDto request, UserRole role)
        {
            try
            {
                // Base mapping query
                var mappingQuery = _context.UserAssessmentMappings
                    .Where(x => !x.IsDeleted);               

                // Apply role-based filtering
                if (role != UserRole.Admin)
                {
                    mappingQuery = role switch
                    {
                        UserRole.Analyst or UserRole.Evaluator => mappingQuery
                            .Where(x => x.UserPillarMappings
                                .Any(up => up.UserID == request.UserId && !up.IsDeleted && up.IsActive)),

                        _ => mappingQuery
                    };
                }

                // Main query (JOIN instead of Contains for better SQL)
                var query =
                    from a in _context.Assessments
                    join m in mappingQuery
                        on a.UserAssessmentMappingID equals m.UserAssessmentMappingID
                    where a.IsActive
                          && (!request.UserAssessmentMappingID.HasValue ||
                              a.UserAssessmentMappingID == request.UserAssessmentMappingID.Value)

                    let responses = a.PillarAssessments
                        .Where(p => !p.IsDeleted)
                        .SelectMany(p => p.Responses)
                        .Where(r => !r.IsDeleted)

                    select new GetAssessmentResponseDto
                    {
                        AssessmentID = a.AssessmentID,
                        UserAssessmentMappingID = a.UserAssessmentMappingID,
                        CreatedAt = a.CreatedAt,
                        GeographicReference = m.GeographicReference,
                        Year = m.Year,
                        UserID = m.UserID,
                        AnalystName = m.User.FullName,
                        Role = m.Role,
                        DueDate = m.DueDate,
                        Score = (responses.Where(r => r.Score.HasValue && (int)r.Score.Value <= (int)ScoreValue.Four).Sum(r => (decimal)((int?)r.Score ?? 0)) * 100m),
                        AssessmentPhase = a.AssessmentPhase,
                    };
                              

                var result =  await query.ApplyPaginationAsync(request);

                foreach (var item in result.Data)
                {
                    var listofPillars = await _context.UserPillarMappings.Where(x => x.UserAssessmentMappingID == item.UserAssessmentMappingID).Select(x => x.PillarID).Distinct().ToListAsync();

                    var totalQuestions = await _context.Questions.Where(q => listofPillars.Contains(q.PillarID) && !q.IsDeleted).CountAsync();

                    item.Score = totalQuestions == 0 ? 0 : Math.Round(item.Score / (totalQuestions * 4m), 2);

                }
                return result;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetAssessmentResult", ex);

                return new PaginationResponse<GetAssessmentResponseDto>
                {
                    Data = new List<GetAssessmentResponseDto>(),
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalRecords = 0    
                };
            }
        }

        public async Task<PaginationResponse<GetAssessmentQuestionResponseDto>?> GetAssessmentQuestion(GetAssessmentQuestionRequestDto request)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(x => x.UserID == request.UserId);
                if (user == null) return null;

                var userIDs = new List<int>();
                var query = _context.Assessments
                    .Include(a => a.PillarAssessments)
                    .ThenInclude(pa => pa.Responses)
                        .ThenInclude(r => r.Question)
                            .ThenInclude(q => q.QuestionOptions)
                    .Where(a => a.AssessmentID == request.AssessmentID)
                    .SelectMany(a => a.PillarAssessments)
                    .Where(x => !request.PillarID.HasValue || x.PillarID == request.PillarID.Value)
                    .SelectMany(x => x.Responses)
                    .Select(r => new GetAssessmentQuestionResponseDto
                    {
                        AssessmentID = request.AssessmentID,
                        PillarID = r.PillarAssessment.PillarID,
                        PillarName = r.Question.Pillar.PillarName,
                        QuestionID = r.QuestionID,
                        Score = r.Score,
                        UserID = user.UserID,
                        Justification = r.Justification,
                        Source = r.Source ?? "",
                        QuestionOptionText = r.Question.QuestionOptions
                            .Where(o => o.OptionID == r.QuestionOptionID)
                            .Select(o => o.OptionText)
                            .FirstOrDefault() ?? string.Empty,
                        QuestionText = r.Question.QuestionText
                    });

                var response = await query.ApplyPaginationAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in GetAssessmentQuestion", ex);
                return new PaginationResponse<GetAssessmentQuestionResponseDto>
                {
                    Data = new List<GetAssessmentQuestionResponseDto>(),
                    PageNumber = 1,
                    PageSize = 10,
                    TotalRecords = 0
                };
            }
        }

        // ────────────────────────────────────────────────────────────
        //  IMPORT
        // ────────────────────────────────────────────────────────────
        private const int FIRST_Q_ROW = 9;
        private const int ROWS_PER_Q = 4;
        public async Task<ResultResponseDto<string>> ImportAssessmentAsync(IFormFile file, int userID, UserRole userRole)
        {
            try
            {
                // Load all options once
                var allOptions = _context.QuestionOptions.ToList();
                int recordSaved = 0;

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                using var workbook = new XLWorkbook(stream);

                foreach (var ws in workbook.Worksheets)
                {
                    // Skip the hidden options data sheet
                    if (ws.Name.StartsWith("__")) continue;

                    // ── Read meta from first question's source row ────
                    // First question: ansRow=9, sourceRow=9+2=11
                    int userAssessmentMappingID = ws.Cell(11, 11).GetValue<int>();
                    int pillarID = ws.Cell(11, 12).GetValue<int>();

                    if (userAssessmentMappingID == 0 || pillarID == 0)
                        continue; // empty or corrupt sheet — skip

                    // Validate that the file belongs to the uploading user
                    if (!_context.UserAssessmentMappings.Any(x =>
                            !x.IsDeleted &&
                            x.UserID == userID &&
                            x.UserAssessmentMappingID == userAssessmentMappingID))
                    {
                        return ResultResponseDto<string>.Failure(new[] { "Invalid file uploaded" });
                    }

                    int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                    var assessmentResponses = new List<AddAssesmentResponseDto>();

                    // ── Walk question blocks (4 rows each, starting row 9) ──
                    for (int row = FIRST_Q_ROW; row <= lastRow - 2; row += ROWS_PER_Q)
                    {
                        int sourceRow = row + 2;

                        int questionID = ws.Cell(sourceRow, 13).GetValue<int?>() ?? 0;
                        int responseID = ws.Cell(sourceRow, 15).GetValue<int?>() ?? 0;

                        // Once we reach rows without question IDs we're past the questions
                        if (questionID == 0) break;

                        string answerText = ws.Cell(row, 4).GetString().Trim(); // dropdown value
                        string comment = ws.Cell(row + 1, 4).GetString().Trim(); // comment
                        string source = ws.Cell(row + 2, 4).GetString().Trim(); // source

                        int? score = null;
                        int matchedOptionID = 0;

                        var qOptions = allOptions.Where(x => x.QuestionID == questionID).ToList();

                        if (!string.IsNullOrWhiteSpace(answerText))
                        {
                            // 1. Exact full-text match against "N - Option text" or plain option text
                            foreach (var opt in qOptions)
                            {
                                string prefix = opt.ScoreValue.HasValue ? $"{opt.ScoreValue} - " : "";
                                string fullText = (prefix + opt.OptionText.Trim()).Trim();

                                if (fullText.Equals(answerText, StringComparison.OrdinalIgnoreCase) ||
                                    opt.OptionText.Trim().Equals(answerText, StringComparison.OrdinalIgnoreCase))
                                {
                                    matchedOptionID = opt.OptionID;
                                    score = opt.ScoreValue.HasValue ? (int?)opt.ScoreValue.Value : null;
                                    break;
                                }
                            }

                            // 2. Fallback: first character is a digit 0-4
                            if (matchedOptionID == 0 &&
                                answerText.Length >= 1 &&
                                int.TryParse(answerText[0].ToString(), out int parsedScore) &&
                                parsedScore >= 0 && parsedScore <= 4)
                            {
                                var fallbackOpt = qOptions.FirstOrDefault(x => x.ScoreValue == parsedScore);
                                if (fallbackOpt != null)
                                {
                                    matchedOptionID = fallbackOpt.OptionID;
                                    score = parsedScore;
                                }
                            }
                        }

                        if (matchedOptionID > 0)
                        {
                            assessmentResponses.Add(new AddAssesmentResponseDto
                            {
                                AssessmentID = 0,
                                QuestionID = questionID,
                                ResponseID = responseID,
                                QuestionOptionID = matchedOptionID,
                                Score = score.HasValue ? (ScoreValue)score.Value : null,
                                Justification = comment,
                                Source = string.IsNullOrWhiteSpace(source) ? null : source
                            });
                        }
                    }

                    // ── Save this pillar's responses ──────────────────


                    var assessment = new AddAssessmentDto
                    {
                        AssessmentID = 0,
                        UserAssessmentMappingID = userAssessmentMappingID,
                        PillarID = pillarID,
                        Responses = assessmentResponses
                    };

                    var response = await SaveAssessment(assessment, userID, userRole);
                    if (!response.Succeeded)
                        return response;

                    recordSaved++;
                }

                return ResultResponseDto<string>.Success("", new[]
                {
                    recordSaved > 0
                    ? $"{recordSaved} Pillar(s) Assessment saved successfully"
                        : "Please fill the sheet properly before submitting"
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in ImportAssessmentAsync", ex);
                return ResultResponseDto<string>.Failure(new[] { "Failed to save assessment" });
            }
        }

        public async Task<GetCityQuestionHistoryReponseDto> GetCityQuestionHistory(UserCityRequstDto userCityRequstDto)
        {
            try
            {
                var userID = userCityRequstDto.UserID;
                var cityID = userCityRequstDto.CityID;

                var user = await _context.Users.FirstOrDefaultAsync(x => x.UserID == userID && x.Role != UserRole.Executive);
                if (user == null)
                {
                    return new GetCityQuestionHistoryReponseDto
                    {
                        CityID = cityID,
                        Score = 0,
                        TotalPillar = 0,
                        TotalAnsPillar = 0,
                        TotalQuestion = 0,
                        AnsQuestion = 0,
                        TotalAssessment = 0,
                        Pillars = new List<CityPillarQuestionHistoryReponseDto>()
                    };
                }
                var cityHistory = new CityHistoryDto();

                Expression<Func<UserAssessmentMapping, bool>> predicate = user.Role switch
                {
                    UserRole.Analyst => x => !x.IsDeleted && x.Year == cityID && (x.AssignedByUserId == userID || x.UserID == userID),
                    UserRole.Evaluator => x => !x.IsDeleted && x.Year == cityID && x.UserID == userID,
                    _ => x => !x.IsDeleted && x.Year == cityID
                };


                // 1. Get all UserAssessmentMapping IDs for the city
                var ucmIds = await _context.UserAssessmentMappings
                    .Where(predicate)
                    .Select(x => x.UserAssessmentMappingID)
                    .ToListAsync();

                var pillarAssessments = _context.Assessments
                    .Where(a => ucmIds.Contains(a.UserAssessmentMappingID) && a.IsActive && a.UpdatedAt.Year == userCityRequstDto.UpdatedAt.Year)
                    .SelectMany(x => x.PillarAssessments);

                // 2. Fetch city-wise pillar/question details in one go
                var cityPillarQuery =
                    from p in _context.Pillars
                    join pa in pillarAssessments on p.PillarID equals pa.PillarID into paGroup
                    from pa in paGroup.DefaultIfEmpty()
                    select new
                    {
                        p.PillarID,
                        p.PillarName,
                        UserID = pa != null && pa.Responses
                                .Where(r => r.Score.HasValue && (int)r.Score.Value <= (int)ScoreValue.Four)
                                .Count() > 0 ? pa.Assessment.UserAssessmentMapping.UserID : 0,
                        Score = pa != null
                            ? pa.Responses
                                .Where(r => r.Score.HasValue && (int)r.Score.Value <= (int)ScoreValue.Four)
                                .Sum(r => (int?)r.Score ?? 0)
                            : 0,
                        ScoreCount = pa != null ? pa.Responses.Where(r => r.Score.HasValue && (int)r.Score.Value <= (int)ScoreValue.Four).Count() : 0,
                        TotalQuestion = p.Questions.Count(),
                        AnsQuestion = pa != null ? pa.Responses.Count() : 0,
                        HasAnswer = pa != null
                    };
                var list = await cityPillarQuery.Distinct().ToListAsync();
                var cityPillars = (list)
                    .GroupBy(x => new { x.PillarID, x.PillarName })
                    .Select(g =>
                    {
                        var totalAnsScoreOfPillar = g.Sum(x => x.Score);
                        var ScoreCount = g.Sum(x => x.ScoreCount);
                        var ansUserCount = g.Where(x => x.UserID > 0).Distinct().Count();
                        var totalQuestionsInPillar = g.Max(x => x.TotalQuestion) * ansUserCount;

                        decimal progress = ScoreCount != 0 && ansUserCount > 0 ? totalAnsScoreOfPillar * 100 / (ScoreCount * 4m ) : 0m;

                        return new CityPillarQuestionHistoryReponseDto
                        {
                            PillarID = g.Key.PillarID,
                            PillarName = g.Key.PillarName,
                            Score = totalAnsScoreOfPillar,
                            ScoreProgress = progress,
                            AnsPillar = g.Sum(x => x.HasAnswer ? 1 : 0),
                            TotalQuestion = totalQuestionsInPillar,
                            AnsQuestion = g.Sum(x => x.AnsQuestion)
                        };
                    })
                    .ToList();

                //// 3. Get assessment count in one query
                //var assessmentCount = await _context.Assessments
                //    .CountAsync(x => ucmIds.Contains(x.UserAssessmentMappingID) && x.IsActive);

                //// 4. Total pillars and questions (static across city)
                //var pillarStats = await _context.Pillars
                //    .Select(p => new { QuestionsCount = p.Questions.Count() })
                //    .ToListAsync();
                //int totalPillars = pillarStats.Count;
                //int totalQuestions = pillarStats.Sum(p => p.QuestionsCount);

                // 5. Final payload
                var payload = new GetCityQuestionHistoryReponseDto
                {
                    CityID = cityID,
                    //TotalAssessment = assessmentCount,
                    //Score = cityPillars.Sum(x => x.Score),
                    ScoreProgress = cityPillars.Average(x => x.ScoreProgress),
                    //TotalPillar = totalPillars * ucmIds.Count,
                    //TotalAnsPillar = cityPillars.Sum(x => x.AnsPillar),
                    //TotalQuestion = totalQuestions * ucmIds.Count,
                    //AnsQuestion = cityPillars.Sum(x => x.AnsQuestion),
                    Pillars = cityPillars
                };

                return payload;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in GetCityQuestionHistory", ex);
                return new GetCityQuestionHistoryReponseDto
                {
                    CityID = 0,
                    Score = 0,
                    TotalPillar = 0,
                    TotalAnsPillar = 0,
                    TotalQuestion = 0,
                    AnsQuestion = 0,
                    TotalAssessment = 0,
                    Pillars = new List<CityPillarQuestionHistoryReponseDto>()
                };
            }
        }

        public async Task<ResultResponseDto<GetAssessmentHistoryDto>> GetAssessmentProgressHistory( int assessmentID, int userID, UserRole userRole)
        {
            try
            {
                // Step 1: Get UserAssessmentMappingID
                var mappingId = await _context.Assessments
                    .Where(a => a.AssessmentID == assessmentID)
                    .Select(a => a.UserAssessmentMappingID)
                    .FirstOrDefaultAsync();

                if (mappingId == 0)
                {
                    return ResultResponseDto<GetAssessmentHistoryDto>
                        .Failure(new[] { "Failed to get assessment history" });
                }

                List<int> accessPillars = new();

                // Step 2: Get accessible pillars
                if (userRole == UserRole.Admin ||
                    userRole == UserRole.Executive)
                {
                    accessPillars = await _context.Pillars                        
                        .Select(p => p.PillarID)
                        .ToListAsync();
                }
                else
                {
                    accessPillars = await _context.UserPillarMappings
                        .Where(x => x.UserAssessmentMappingID == mappingId
                                 && x.UserID == userID
                                 && !x.IsDeleted
                                 && x.IsActive)
                        .Select(x => x.PillarID)
                        .ToListAsync();

                    if (!accessPillars.Any())
                    {
                        return ResultResponseDto<GetAssessmentHistoryDto>
                            .Success(new GetAssessmentHistoryDto
                            {
                                AssessmentID = assessmentID,
                                Score = 0,
                                TotalAnsPillar = 0,
                                TotalPillar = 0,
                                TotalAnsQuestion = 0,
                                TotalQuestion = 0,
                                CurrentProgress = 0
                            }, new[] { "No accessible pillars found" });
                    }
                }

                // Step 3: Build query
                var pillarAssessmentQuery = _context.PillarAssessments
                    .Where(pa => pa.AssessmentID == assessmentID);

                if (userRole != UserRole.Admin &&
                    userRole != UserRole.Executive)
                {
                    pillarAssessmentQuery = pillarAssessmentQuery
                        .Where(pa => accessPillars.Contains(pa.PillarID));
                }

                var data = await pillarAssessmentQuery
                    .Select(pa => new
                    {
                        pa.PillarID,
                        Responses = pa.Responses
                            .Where(r => r.Score.HasValue)
                    })
                    .ToListAsync();

                // Step 4: Total Questions
                int totalQuestions;

                if (userRole == UserRole.Admin ||
                    userRole == UserRole.Executive)
                {
                    totalQuestions = await _context.Questions
                        .CountAsync();
                }
                else
                {
                    totalQuestions = await _context.Questions
                        .Where(q => accessPillars.Contains(q.PillarID))
                        .CountAsync();
                }

                // Step 5: Calculations
                var totalAnsweredQuestions = data
                    .SelectMany(p => p.Responses)
                    .Count();

                var score = data
                    .SelectMany(p => p.Responses)
                    .Where(r => r.Score!.Value <= ScoreValue.Four)
                    .Sum(r => (int)r.Score!.Value);

                var totalAnsweredPillars = data
                    .Count(p => p.Responses.Any());

                int totalPillars;

                if (userRole == UserRole.Admin ||
                    userRole == UserRole.Executive)
                {
                    totalPillars = await _context.Pillars                       
                        .CountAsync();
                }
                else
                {
                    totalPillars = accessPillars.Count;
                }

                // Step 6: Response
                var result = new GetAssessmentHistoryDto
                {
                    AssessmentID = assessmentID,
                    Score = score,
                    TotalAnsPillar = totalAnsweredPillars,
                    TotalPillar = totalPillars,
                    TotalAnsQuestion = totalAnsweredQuestions,
                    TotalQuestion = totalQuestions,
                    CurrentProgress = totalQuestions > 0
                        ? Math.Round(
                            (totalAnsweredQuestions / (double)totalQuestions) * 100,
                            0)
                        : 0
                };

                return ResultResponseDto<GetAssessmentHistoryDto>
                    .Success(
                        result,
                        new[] { "Assessment history fetched successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync(
                    "Error in GetAssessmentProgressHistory",
                    ex);

                return ResultResponseDto<GetAssessmentHistoryDto>
                    .Failure(new[] { "Failed to get assessment history" });
            }
        }

        public async Task<ResultResponseDto<string>> ChangeAssessmentStatus(ChangeAssessmentStatusRequestDto r)
        {
            try
            {
                var assessment = await _context.Assessments.FirstOrDefaultAsync(x=>x.AssessmentID == r.AssessmentID);
                if(assessment != null)
                {
                    assessment.AssessmentPhase = r.AssessmentPhase;

                    _context.Assessments.Update(assessment);
                    await _context.SaveChangesAsync();

                    return ResultResponseDto<string>.Success("", new[] { "Assessment Status Changed successfully" });
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in ChangeAssessmentStatus", ex);
                return ResultResponseDto<string>.Failure(new[] { "Failed to Changed assessment status" });

            }
            return ResultResponseDto<string>.Failure(new[] { "Failed to Changed assessment status" });
        }

        public async Task<ResultResponseDto<string>> TransferAssessment(TransferAssessmentRequestDto r)
        {
            try
            {
                var currentDate = DateTime.UtcNow;

                var transferAssessment = await _context.Assessments
                    .Include(x => x.UserAssessmentMapping)
                    .Include(x => x.PillarAssessments)
                        .ThenInclude(x => x.Responses)
                    .FirstOrDefaultAsync(x => x.AssessmentID == r.AssessmentID);

                if (transferAssessment == null)
                    return ResultResponseDto<string>.Failure(new[] { "Invalid assessment." });

                var cityAssigned = await _context.UserAssessmentMappings
                    .FirstOrDefaultAsync(x => x.CityID == transferAssessment.UserAssessmentMapping.Year &&
                                              x.UserID == r.TransferToUserID);

                if (cityAssigned == null)
                    return ResultResponseDto<string>.Failure(new[] { "This assessment can’t be imported because the selected user hasn’t been assigned to this city yet." });

                // Load existing assessment for that user/city/year (with pillars/responses)
                var existingAssessment = await _context.Assessments
                    .Include(a => a.PillarAssessments)
                        .ThenInclude(p => p.Responses)
                        .ThenInclude(r=>r.AssessmentResponseHistories)
                    .FirstOrDefaultAsync(a => a.UserAssessmentMappingID == cityAssigned.UserAssessmentMappingID &&
                                              a.UpdatedAt.Year == currentDate.Year);

                if (existingAssessment == null)
                {
                    existingAssessment = new Assessment
                    {
                        UserAssessmentMappingID = cityAssigned.UserAssessmentMappingID,
                        CreatedAt = currentDate,
                        UpdatedAt = currentDate,
                        IsActive = true,
                        AssessmentPhase = transferAssessment.AssessmentPhase == AssessmentPhase.Completed ?transferAssessment.AssessmentPhase: AssessmentPhase.InProgress,
                        PillarAssessments = new List<PillarAssessment>()
                    };

                    _context.Assessments.Add(existingAssessment);
                }
                else
                {
                    existingAssessment.UpdatedAt = currentDate;
                    existingAssessment.AssessmentPhase = transferAssessment.AssessmentPhase == AssessmentPhase.Completed ? transferAssessment.AssessmentPhase : AssessmentPhase.InProgress;
                }

                // Transfer pillar data
                foreach (var pillar in transferAssessment.PillarAssessments)
                {
                    var existingPillar = existingAssessment.PillarAssessments
                        .FirstOrDefault(x => x.PillarID == pillar.PillarID);

                    if (existingPillar == null)
                    {
                        existingPillar = new PillarAssessment
                        {
                            PillarID = pillar.PillarID,
                            Responses = new List<AssessmentResponse>()
                        };
                        existingAssessment.PillarAssessments.Add(existingPillar);
                    }

                    // Add/Update responses
                    foreach (var response in pillar.Responses)
                    {
                        var existingResponse = existingPillar.Responses
                            .FirstOrDefault(rp => rp.QuestionID == response.QuestionID);

                        if (existingResponse == null)
                        {
                            existingPillar.Responses.Add(new AssessmentResponse
                            {
                                QuestionID = response.QuestionID,
                                QuestionOptionID = response.QuestionOptionID,
                                Justification = response.Justification,
                                Score = response.Score
                            });
                        }
                        else
                        {
                            existingResponse.QuestionOptionID = response.QuestionOptionID;
                            existingResponse.Justification = response.Justification;
                            existingResponse.Score = response.Score;
                        }
                    }

                    // Delete responses not present in transferAssessment
                    var transferQuestionIds = pillar.Responses.Select(x => x.QuestionID).ToHashSet();
                    var toDeleteResponses = existingPillar.Responses
                        .Where(x => !transferQuestionIds.Contains(x.QuestionID))
                        .ToList();

                    foreach (var resp in toDeleteResponses)
                    {
                        resp.IsDeleted = true;
                        resp.UpdatedAt = currentDate;
                        _context.AssessmentResponses.Update(resp);
                    }
                }

                // Delete pillars not present in transferAssessment
                var transferPillarIds = transferAssessment.PillarAssessments.Select(x => x.PillarID).ToHashSet();
                var toDeletePillars = existingAssessment.PillarAssessments
                    .Where(x => !transferPillarIds.Contains(x.PillarID))
                    .ToList();

                foreach (var pillar in toDeletePillars)
                {
                    pillar.UpdatedAt = currentDate;
                    _context.PillarAssessments.Update(pillar);
                }
                _download.InsertAnalyticalLayerResults(transferAssessment.UserAssessmentMapping.Year);
                await _context.SaveChangesAsync();

                return ResultResponseDto<string>.Success("", new[] { "Assessment transferred successfully." });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in TransferAssessment", ex);
                return ResultResponseDto<string>.Failure(new[] { "Failed to transfer assessment, please try again later." });
            }
        }
        public async Task<ResultResponseDto<AiCityPillarDashboardResponseDto>> GetCityPillarHistory(UserCityDashBoardRequstDto request, int userId, UserRole userRole)
        {
            try
            {
                var year = request.UpdatedAt.Year;

                // 1. Validate city access
                var hasAccess = await _context.UserAssessmentMappings
                    .AnyAsync(x =>
                        !x.IsDeleted &&
                        (userRole == UserRole.Admin ||
                         (x.UserID == userId && x.CityID == request.CityID)));

                if (!hasAccess)
                {
                    return ResultResponseDto<AiCityPillarDashboardResponseDto>
                        .Failure(new[] { "Unauthorized or invalid city access" });
                }

                // 2. Fetch required data in parallel
                var pillarEvaluationsList = await _commonService
                    .GetCitiesProgressAsync(userId, (int)userRole, year);

                var pillars = await _context.Pillars
                    .AsNoTracking()
                    .Select(P=>new
                    {
                        P,
                        TotalQuestions = P.Questions.Count
                    })
                    .OrderBy(x => x.P.DisplayOrder)
                    .ToListAsync();

                var aiCityProgress = await _context.AICityScores
                    .Where(x => x.CityID == request.CityID && x.Year == year)
                    .MaxAsync(x => x.AIProgress);

                var city = await _context.Cities
                    .AsNoTracking()
                    .Where(x => x.CityID == request.CityID)
                    .Select(x => new { x.CityID, x.CityName })
                    .FirstOrDefaultAsync();

                var pillarEvaluations = pillarEvaluationsList.Where(x => x.UserAssessmentMappingID == request.CityID);

                // 3. Map pillar results
                var pillarResults = pillars
                    .GroupJoin(
                        pillarEvaluations,
                        p => p.P.PillarID,
                        e => e.PillarID,
                        (pillar, evals) => new CityPillarDashboardPillarValueDto
                        {
                            PillarID = pillar.P.PillarID,
                            PillarName = pillar.P.PillarName,
                            DisplayOrder = pillar.P.DisplayOrder,
                            ScoreProgress = evals.FirstOrDefault()?.ScoreProgress ?? 0
                        })
                    .ToList();

                // 4. Prepare response
                var response = new AiCityPillarDashboardResponseDto
                {
                    //CityID = request.CityID,
                    //CityName = city?.CityName ?? string.Empty,
                    //AiValue = aiCityProgress ?? 0,
                    ScoreProgress = Math.Round(pillarEvaluations.Average(x => x.ScoreProgress), 2),
                    Pillars = pillarResults
                };

                return ResultResponseDto<AiCityPillarDashboardResponseDto>
                    .Success(response, new[] { "Pillars fetched successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync(nameof(GetCityPillarHistory), ex);

                return ResultResponseDto<AiCityPillarDashboardResponseDto>
                    .Failure(new[] { "Error in getting pillar details" });
            }
        }
        public async Task<ResultResponseDto<List<GetAssignedAssessmentResponseDto>>> GetAssignedAssessments(int userID, UserRole userRole)
        {
            try
            {
                var assessments = await _context.UserAssessmentMappings
                    .Include(x => x.UserPillarMappings)
                    .Where(x => x.UserID == userID && !x.IsDeleted && x.IsActive)
                    .Select(x => new GetAssignedAssessmentResponseDto
                    {
                        UserAssessmentMappingID = x.UserAssessmentMappingID,
                        UserID = x.UserID,
                        Year = x.Year,
                        DueDate = x.DueDate,
                        UpdatedAt = DateTime.UtcNow,
                        GeographicReference = x.GeographicReference,

                        UserPillarMappings = x.UserPillarMappings.Where(x => !x.IsDeleted && x.IsActive && x.UserID == userID)
                        .Select(y => new AssignedAssessmentPillarMappingDto
                        {
                            UserPillarMappingID = y.UserPillarMappingID,
                            UserID = y.UserID,
                            Year = y.Year,
                            DueDate = y.DueDate,
                            PillarID = y.PillarID,
                            PillarName = y.Pillar.PillarName,
                            Description = y.Pillar.Description,
                            DisplayOrder = y.Pillar.DisplayOrder
                        }).ToList()

                    }).ToListAsync();

                return ResultResponseDto<List<GetAssignedAssessmentResponseDto>>
                    .Success(assessments, new[] { "Pillars fetched successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync(nameof(GetCityPillarHistory), ex);

                return ResultResponseDto<List<GetAssignedAssessmentResponseDto>>
                    .Failure(new[] { "Error in getting pillar details" });
            }
        }
        public async Task<ResultResponseDto<List<GetAssignedAssessmentResponseDto>>> GetAssignedInvitations(int userID, UserRole userRole)
        {
            try
            {
                List<GetAssignedAssessmentResponseDto> data;

                if (userRole == UserRole.Admin || userRole == UserRole.Executive)
                {
                    data = await _context.UserAssessmentMappings
                        .Where(x => !x.IsDeleted && x.IsActive)
                        .Select(upm => new GetAssignedAssessmentResponseDto
                        {
                            UserAssessmentMappingID = upm.UserAssessmentMappingID,
                            UserID = upm.UserID,
                            Year = upm.Year,
                            DueDate = upm.DueDate,
                            UpdatedAt = upm.UpdatedAt,

                            AssignedBy = upm.User.FullName,

                            GeographicReference = upm.GeographicReference,

                            UserPillarMappings = upm.UserPillarMappings
                                .Where(p => !p.IsDeleted && p.IsActive && p.UserID == upm.UserID)
                                .Select(p => new AssignedAssessmentPillarMappingDto
                                {
                                    UserPillarMappingID = p.UserPillarMappingID,
                                    UserID = p.UserID,
                                    Year = p.Year,
                                    DueDate = p.DueDate,
                                    PillarID = p.PillarID,
                                    PillarName = p.Pillar.PillarName,
                                    Description = p.Pillar.Description,
                                    DisplayOrder = p.Pillar.DisplayOrder,
                                    ImagePath = p.Pillar.ImagePath
                                })
                                .OrderBy(p => p.DisplayOrder)
                                .ToList()
                        })
                        .OrderByDescending(x => x.Year)
                        .ToListAsync();
                }
                else
                {
                    data = await _context.UserPillarMappings
                        .Where(x => x.UserID == userID && !x.IsDeleted && x.IsActive)
                        .GroupBy(x => new
                        {
                            x.UserAssessmentMappingID,
                            x.Year,
                            x.UserAssessmentMapping.GeographicReference,
                            x.AssignedByUserId
                        })
                        .Select(g => new GetAssignedAssessmentResponseDto
                        {
                            UserAssessmentMappingID = g.Key.UserAssessmentMappingID,
                            UserID = g.Max(x => x.UserID),
                            Year = g.Key.Year,

                            DueDate = g.Max(x => x.DueDate),
                            UpdatedAt = g.Max(x => x.UpdatedAt),

                            AssignedBy = _context.Users
                                .Where(u => u.UserID == g.Key.AssignedByUserId)
                                .Select(u => u.FullName)
                                .FirstOrDefault() ?? "",

                            GeographicReference = g.Key.GeographicReference,

                            UserPillarMappings = g.Select(p => new AssignedAssessmentPillarMappingDto
                            {
                                UserPillarMappingID = p.UserPillarMappingID,
                                UserID = p.UserID,
                                Year = p.Year,
                                DueDate = p.DueDate,
                                PillarID = p.PillarID,
                                PillarName = p.Pillar.PillarName,
                                Description = p.Pillar.Description,
                                DisplayOrder = p.Pillar.DisplayOrder,
                                ImagePath = p.Pillar.ImagePath
                            })
                            .OrderBy(p => p.DisplayOrder)
                            .ToList()
                        })
                        .OrderByDescending(x => x.Year)
                        .ToListAsync();
                }

                return ResultResponseDto<List<GetAssignedAssessmentResponseDto>>
                    .Success(data, new[] { "User Assessment fetched successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("GetAssignedInvitations", ex);

                return ResultResponseDto<List<GetAssignedAssessmentResponseDto>>
                    .Failure(new[] { "Error while fetching assigned assessments" });
            }
        }



        public async Task<ResultResponseDto<List<GetExecutiveAssignedAssessmentResponseDto>>> GetExecutiveAssignedInvitations(
            int userID, UserRole userRole, string? searchText)
        {
            try
            {
                if (userRole != UserRole.Admin && userRole != UserRole.Executive)
                {
                    return ResultResponseDto<List<GetExecutiveAssignedAssessmentResponseDto>>
                        .Success(new List<GetExecutiveAssignedAssessmentResponseDto>(),
                            new[] { "User Assessment fetched successfully" });
                }

                searchText = searchText?.Trim();

                var pillarEvaluationsList = await _commonService
                    .GetAssessmentProgressAsync(userID, (int)userRole);

                var evalByMappingAndPillar = pillarEvaluationsList
                    .GroupBy(x => (x.UserAssessmentMappingID, x.PillarID))
                    .ToDictionary(g => g.Key, g => g.ToList());

                var evalsByMapping = pillarEvaluationsList
                    .GroupBy(x => x.UserAssessmentMappingID)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var pillarMeta = await _context.Pillars
                    .AsNoTracking()
                    .Select(p => new
                    {
                        p.PillarID,
                        p.PillarName,
                        p.Description,
                        p.DisplayOrder,
                        p.ImagePath,
                        TotalQuestions = p.Questions.Count(q => !q.IsDeleted),
                        TotalCriticalQuestions = p.Questions.Count(q => !q.IsDeleted && q.IsCritical)
                    })
                    .ToDictionaryAsync(p => p.PillarID);

                var mappingQuery = _context.UserAssessmentMappings
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.IsActive);

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    mappingQuery = mappingQuery.Where(x =>
                        EF.Functions.Like(x.GeographicReference, $"%{searchText}%"));
                }

                var mappings = await mappingQuery
                    .OrderByDescending(x => x.Year)
                    .Select(upm => new
                    {
                        upm.UserAssessmentMappingID,
                        upm.UserID,
                        upm.Year,
                        upm.DueDate,
                        upm.UpdatedAt,
                        AssignedBy = upm.User.FullName,
                        upm.GeographicReference,
                        PillarMappings = upm.UserPillarMappings
                            .Where(p => !p.IsDeleted && p.IsActive && p.UserID == upm.UserID)
                            .Select(p => new
                            {
                                p.UserPillarMappingID,
                                p.UserID,
                                p.Year,
                                p.DueDate,
                                p.PillarID
                            })
                            .ToList()
                    })
                    .ToListAsync();

                var result = new List<GetExecutiveAssignedAssessmentResponseDto>(mappings.Count);

                foreach (var item in mappings)
                {
                    evalsByMapping.TryGetValue(item.UserAssessmentMappingID, out var mappingEvals);
                    mappingEvals ??= new List<EvaluationCityProgressResultDto>();

                    var pillars = item.PillarMappings
                        .Select(p =>
                        {
                            pillarMeta.TryGetValue(p.PillarID, out var meta);
                            evalByMappingAndPillar.TryGetValue(
                                (item.UserAssessmentMappingID, p.PillarID), out var evals);
                            evals ??= new List<EvaluationCityProgressResultDto>();

                            var avgTotalAns = evals.Count > 0 ? evals.Average(x => x.TotalAns) : 0;
                            var avgTotalCriAns = evals.Count > 0
                                ? evals.Average(x => x.TotalAnsweredCriticalQuestions)
                                : 0;

                            return new AssignedAssessmentPillarMappingDto
                            {
                                UserPillarMappingID = p.UserPillarMappingID,
                                UserID = p.UserID,
                                Year = p.Year,
                                DueDate = p.DueDate,
                                PillarID = p.PillarID,
                                PillarName = meta?.PillarName ?? string.Empty,
                                Description = meta?.Description ?? string.Empty,
                                DisplayOrder = meta?.DisplayOrder ?? 0,
                                ImagePath = meta?.ImagePath ?? string.Empty,
                                TotalScore = evals.Sum(x => x.TotalScore),
                                TotalAnsweredQuestions = (int)Math.Round(avgTotalAns, MidpointRounding.AwayFromZero),
                                TotalCriticalAnsweredQuestions = (int)Math.Round(avgTotalCriAns, MidpointRounding.AwayFromZero),
                                TotalQuestions = evals.Count > 0
                                    ? (int)Math.Round(evals.Average(x => x.TotalQuestions), MidpointRounding.AwayFromZero)
                                    : meta?.TotalQuestions ?? 0,
                                TotalCriticalQuestions = evals.Count > 0
                                    ? (int)Math.Round(evals.Average(x => x.TotalCriticalQuestions), MidpointRounding.AwayFromZero)
                                    : meta?.TotalCriticalQuestions ?? 0,
                                CompletionRate = evals.Count > 0 ? evals.Average(x => x.CompletionRate) : 0,
                                ScoreProgress = evals.Count > 0 ? evals.Average(x => x.ScoreProgress) : 0
                            };
                        })
                        .OrderBy(p => p.DisplayOrder)
                        .ToList();

                    var pillarCount = pillars.Count;
                    var totalScore = pillars.Sum(x => x.TotalScore);
                    var totalAnswered = pillars.Sum(x => x.TotalAnsweredQuestions);
                    var totalCriticalAnswered = pillars.Sum(x => x.TotalCriticalAnsweredQuestions);
                    var totalQuestions = pillars.Sum(x => x.TotalQuestions);
                    var totalCriticalQuestions = pillars.Sum(x => x.TotalCriticalQuestions);

                    var progress = totalQuestions == 0 ? 0 : (totalAnswered * 100m) / totalQuestions;

                    var totalDays = (item.DueDate - item.UpdatedAt)?.Days ?? 0;
                    var daysElapsed = (DateTime.UtcNow - item.UpdatedAt)?.Days ?? 0;
                    if (totalDays <= 0) totalDays = 1;
                    if (daysElapsed < 0) daysElapsed = 0;

                    var expectedQuestions = (daysElapsed * totalQuestions) / totalDays;
                    if (expectedQuestions > totalQuestions)
                        expectedQuestions = totalQuestions;

                    var onTrackCount = Math.Min(totalAnswered, expectedQuestions);
                    var offTrackCount = totalAnswered < expectedQuestions
                        ? expectedQuestions - totalAnswered
                        : 0;
                    var atRiskCount = totalQuestions - (onTrackCount + offTrackCount);

                    decimal onTrackPercent = totalQuestions == 0 ? 0 : (onTrackCount * 100m) / totalQuestions;
                    decimal offTrackPercent = totalQuestions == 0 ? 0 : (offTrackCount * 100m) / totalQuestions;
                    decimal atRiskPercent = totalQuestions == 0 ? 0 : (atRiskCount * 100m) / totalQuestions;

                    if (item.DueDate.HasValue && DateTime.UtcNow > item.DueDate)
                    {
                        onTrackCount = totalAnswered;
                        offTrackCount = totalQuestions - totalAnswered;
                        atRiskCount = 0;

                        onTrackPercent = totalQuestions == 0 ? 0 : (onTrackCount * 100m) / totalQuestions;
                        offTrackPercent = totalQuestions == 0 ? 0 : (offTrackCount * 100m) / totalQuestions;
                        atRiskPercent = 0;
                    }

                    var daysRemaining = (item.DueDate - DateTime.UtcNow)?.Days ?? 0;
                    var expected = totalDays == 0 ? 0 : (daysElapsed * 100m) / totalDays;

                    string riskLevel;
                    if (!item.DueDate.HasValue)
                    {
                        riskLevel = "On Track";
                        daysRemaining = 0;
                    }
                    else if (DateTime.UtcNow > item.DueDate)
                    {
                        riskLevel = "Overdue";
                    }
                    else if (progress < expected && daysRemaining <= 3)
                    {
                        riskLevel = "High Risk";
                    }
                    else if (progress < expected)
                    {
                        riskLevel = "At Risk";
                    }
                    else if (daysRemaining <= 3)
                    {
                        riskLevel = "Due Soon";
                    }
                    else
                    {
                        riskLevel = "On Track";
                    }

                    var bestPillar = pillars.OrderByDescending(x => x.CompletionRate).FirstOrDefault();
                    var worstPillar = pillars.OrderBy(x => x.CompletionRate).FirstOrDefault();

                    result.Add(new GetExecutiveAssignedAssessmentResponseDto
                    {
                        UserAssessmentMappingID = item.UserAssessmentMappingID,
                        UserID = item.UserID,
                        Year = item.Year,
                        DueDate = item.DueDate,
                        UpdatedAt = item.UpdatedAt,
                        AssignedBy = item.AssignedBy,
                        GeographicReference = item.GeographicReference,
                        AvgTotalScore = pillarCount == 0
                            ? 0
                            : (int)Math.Round(totalScore / (decimal)pillarCount),
                        TotalAnsweredQuestions = totalAnswered,
                        TotalQuestions = totalQuestions,
                        TotalCriticalAnsweredQuestions = totalCriticalAnswered,
                        TotalCriticalQuestions = totalCriticalQuestions,
                        AvgCompletionRate = mappingEvals.Count > 0
                            ? mappingEvals.Average(x => x.CompletionRate)
                            : 0,
                        AvgScoreProgress = mappingEvals.Count > 0
                            ? Math.Round(mappingEvals.Average(x => x.ScoreProgress), 2)
                            : 0,
                        BestPerformingPillar = bestPillar?.PillarName ?? "",
                        WorstPerformingPillar = worstPillar?.PillarName ?? "",
                        BestCompletionRate = bestPillar?.CompletionRate ?? 0,
                        WorstCompletionRate = worstPillar?.CompletionRate ?? 0,
                        RiskLevel = riskLevel,
                        DaysRemaining = daysRemaining,
                        Progress = Math.Round(progress, 2),
                        OnTrackPercent = Math.Round(onTrackPercent, 2),
                        OffTrackPercent = Math.Round(offTrackPercent, 2),
                        AtRiskPercent = Math.Round(atRiskPercent, 2),
                    });
                }

                var orderedResult = result
                    .OrderBy(x => x.RiskLevel == "Overdue" ? 1 :
                                  x.RiskLevel == "High Risk" ? 2 :
                                  x.RiskLevel == "At Risk" ? 3 :
                                  x.RiskLevel == "Due Soon" ? 4 :
                                  5)
                    .ThenBy(x => x.DaysRemaining)
                    .ToList();

                return ResultResponseDto<List<GetExecutiveAssignedAssessmentResponseDto>>
                    .Success(orderedResult, new[] { "User Assessment fetched successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("GetExecutiveAssignedAssignedInvitations", ex);

                return ResultResponseDto<List<GetExecutiveAssignedAssessmentResponseDto>>
                    .Failure(new[] { "Error while fetching assigned assessments" });
            }
        }


   
        public async Task<ResultResponseDto<AiCityPillarDashboardResponseDto>> GetDashboardPillarHistory(UserDashBoardRequstDto request, int userId, UserRole userRole)
        {
            try
            {
                var pillarAssessment = await _commonService.GetPillarAssessmentProgressResults(userId, (int)userRole, request.UserAssessmentMappingID.GetValueOrDefault());

                if (pillarAssessment == null)
                {
                    return ResultResponseDto<AiCityPillarDashboardResponseDto>
                        .Failure(new[] { "Error in getting pillar details" });
                }

                var groupedPillarAssessment = request.UserAssessmentMappingID is > 0
                    ? pillarAssessment
                        .GroupBy(x => x.UserAssessmentMappingID)
                        .SelectMany(mappingGroup => mappingGroup
                            .GroupBy(x => x.PillarID)
                            .Select(AggregatePillarAssessmentByPillar))
                        .OrderBy(x => x.DisplayOrder ?? 0)
                        .ToList()
                    : pillarAssessment
                        .GroupBy(x => x.PillarID)
                        .Select(AggregatePillarAssessmentByPillar)
                        .OrderBy(x => x.DisplayOrder ?? 0)
                        .ToList();

                var pillarResults = groupedPillarAssessment
                    .Select(pillar => new CityPillarDashboardPillarValueDto
                    {
                        PillarID = pillar.PillarID,
                        PillarName = pillar.PillarName ?? "",
                        DisplayOrder = pillar.DisplayOrder ?? 0,
                        ScoreProgress = pillar.ScoreProgress ?? 0,
                        TotalAns = pillar.TotalAns,
                        TotalQuestions = pillar.TotalQuestions,
                        TotalCriticalQuestions = pillar.TotalCriticalQuestions,
                        TotalAnsweredCriticalQuestions = pillar.TotalAnsweredCriticalQuestions,
                        CompletionRate = pillar.CompletionRate ?? 0,
                        TotalScore = pillar.TotalScore
                    })
                    .ToList();

                var response = new AiCityPillarDashboardResponseDto
                {
                    UserAssessmentMappingID = request.UserAssessmentMappingID,
                    GeographicReference = request.UserAssessmentMappingID is > 0
                        ? groupedPillarAssessment.FirstOrDefault()?.GeographicReference ?? ""
                        : "",
                    ScoreProgress = groupedPillarAssessment.Count > 0
                        ? Math.Round(groupedPillarAssessment.Average(x => x.ScoreProgress ?? 0), 2)
                        : 0,
                    Pillars = pillarResults
                };

                return ResultResponseDto<AiCityPillarDashboardResponseDto>
                    .Success(response, new[] { "Pillars fetched successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync(nameof(GetCityPillarHistory), ex);

                return ResultResponseDto<AiCityPillarDashboardResponseDto>
                    .Failure(new[] { "Error in getting pillar details" });
            }
        }

        private static PillarAssessmentProgressResult AggregatePillarAssessmentByPillar(
            IGrouping<int, PillarAssessmentProgressResult> group)
        {
            var first = group.First();
            var count = group.Count();

            return new PillarAssessmentProgressResult
            {
                PillarID = group.Key,
                PillarName = first.PillarName,
                DisplayOrder = first.DisplayOrder,
                UserAssessmentMappingID = first.UserAssessmentMappingID,
                GeographicReference = first.GeographicReference,
                TotalScore = group.Sum(x => x.TotalScore),
                TotalAns = count > 0
                    ? (int)Math.Round(group.Average(x => x.TotalAns), MidpointRounding.AwayFromZero)
                    : 0,
                TotalQuestions = count > 0
                    ? (int)Math.Round(group.Average(x => x.TotalQuestions), MidpointRounding.AwayFromZero)
                    : 0,
                TotalCriticalQuestions = count > 0
                    ? (int)Math.Round(group.Average(x => x.TotalCriticalQuestions), MidpointRounding.AwayFromZero)
                    : 0,
                TotalAnsweredCriticalQuestions = count > 0
                    ? (int)Math.Round(group.Average(x => x.TotalAnsweredCriticalQuestions), MidpointRounding.AwayFromZero)
                    : 0,
                ScoreProgress = count > 0 ? group.Average(x => x.ScoreProgress ?? 0) : 0,
                CompletionRate = count > 0 ? group.Average(x => x.CompletionRate ?? 0) : 0
            };
        }

    }
}