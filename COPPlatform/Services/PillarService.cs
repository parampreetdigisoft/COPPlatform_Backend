using ClosedXML.Excel;
using COPPlatform.Backgroundjob;
using COPPlatform.Common.Interface;
using COPPlatform.Common.Models;
using COPPlatform.Data;
using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.CommonDto;
using COPPlatform.Dtos.PillarDto;
using COPPlatform.IServices;
using COPPlatform.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace COPPlatform.Services
{
    public class PillarService : IPillarService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAppLogger _appLogger;
        private readonly Download _download;
        private readonly ICommonService _commonService;
        public PillarService(ApplicationDbContext context, IAppLogger appLogger, Download download, ICommonService commonService)
        {
            _context = context;
            _appLogger = appLogger;
            _download = download;
            _commonService = commonService;
        }

        public async Task<List<Pillar>> GetAllAsync()
        {
            try
            {
                return await _context.Pillars.OrderBy(p => p.DisplayOrder).ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in GetAllAsync", ex);
                return new List<Pillar>();
            }

        }

        public async Task<Pillar> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Pillars.FindAsync(id);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in GetByIdAsync", ex);
                return new Pillar();
            }

        }

        public async Task<Pillar> AddAsync(Pillar pillar)
        {
            try
            {
                _context.Pillars.Add(pillar);
                await _context.SaveChangesAsync();
                return pillar;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in AddAsync", ex);
                return new Pillar();
            }

        }

        public async Task<Pillar> UpdateAsync(int id, UpdatePillarDto pillar)
        {
            try
            {
                var existing = await _context.Pillars.FindAsync(id);
                if (existing == null) return null;

                if(existing.IsLocked != pillar.IsLocked)
                {
                    var pillars = _context.UserPillarMappings.Where(x => x.PillarID == id && !x.IsDeleted);

                    foreach (var p in pillars)
                    {
                        p.IsActive = !pillar.IsLocked;
                    }
                }                            

                existing.PillarName = pillar.PillarName;
                existing.Description = pillar.Description;
                existing.DisplayOrder = pillar.DisplayOrder;
                existing.IsLocked = pillar.IsLocked;

                if (existing.Weight != pillar.Weight || existing.Reliability != pillar.Reliability)
                {
                    existing.Weight = pillar.Weight;
                    existing.Reliability = pillar.Reliability;
                    _download.InsertAnalyticalLayerResults();
                }                

                await _context.SaveChangesAsync();

                return existing;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure", ex);
                return new Pillar();
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var pillar = await _context.Pillars.FindAsync(id);
                if (pillar == null) return false;
                _context.Pillars.Remove(pillar);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure", ex);
                return false;
            }

        }

        public async Task<ResultResponseDto<List<PillarWithQuestionsDto>>> GetPillarsWithQuestions(GetCityPillarHistoryRequestDto request)
        {
            try
            {
                // 1. Validate user
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserID == request.UserID);

                if (user == null)
                    return ResultResponseDto<List<PillarWithQuestionsDto>>.Failure(new[] { "Invalid user" });

                // 2. Filter user-city mappings based on role
                Expression<Func<UserAssessmentMapping, bool>> predicate = user.Role switch
                {
                    UserRole.Analyst => x => !x.IsDeleted && x.CityID == request.CityID &&
                                             (x.AssignedByUserId == request.UserID),
                    UserRole.Evaluator => x => !x.IsDeleted && x.CityID == request.CityID && x.UserID == request.UserID,
                    _ => x => !x.IsDeleted && x.CityID == request.CityID
                };

                var mappingIds = await _context.UserAssessmentMappings
                    .Where(predicate)
                    .Select(x => x.UserAssessmentMappingID)
                    .ToListAsync();

                // 3. Get assessments with pillar + responses
                var assessments = await _context.Assessments
                    .Include(a => a.UserAssessmentMapping)
                    .Include(a => a.PillarAssessments)
                        .ThenInclude(pa => pa.Responses)
                    .Where(a => mappingIds.Contains(a.UserAssessmentMappingID) && a.IsActive && a.UpdatedAt.Year == request.UpdatedAt.Year && (a.AssessmentPhase == AssessmentPhase.Completed || a.AssessmentPhase == AssessmentPhase.EditRejected || a.AssessmentPhase == AssessmentPhase.EditRequested))
                    .AsNoTracking()
                    .ToListAsync();

                // 4. Get pillar list with questions + options
                var pillars = await _context.Pillars
                    .Include(p => p.Questions)
                        .ThenInclude(q => q.QuestionOptions)
                    .Where(p => !request.PillarID.HasValue || p.PillarID == request.PillarID)
                    .OrderBy(p => p.DisplayOrder)
                    .AsNoTracking()
                    .ToListAsync();

                // 5. Preload users dictionary
                var userIds = assessments.Select(a => a.UserAssessmentMapping.UserID).Distinct().ToList();
                var usersDict = await _context.Users
                    .Where(u => userIds.Contains(u.UserID))
                    .ToDictionaryAsync(u => u.UserID, u => u.FullName);

                // 6. Build response
                var result = pillars.Select(p => new PillarWithQuestionsDto
                {
                    PillarID = p.PillarID,
                    PillarName = p.PillarName,
                    DisplayOrder = p.DisplayOrder,
                    TotalQuestions = p.Questions.Count,
                    Questions = p.Questions
                        .OrderBy(q => q.DisplayOrder)
                        .Where(q=>!q.IsDeleted)
                        .Select(q =>
                        {
                            var userAnswers = userIds.Select(uid =>
                            {
                                var paResponses = assessments
                                    .Where(a => a.UserAssessmentMapping.UserID == uid)
                                    .SelectMany(a => a.PillarAssessments)
                                    .Where(pa => pa.PillarID == p.PillarID)
                                    .SelectMany(pa => pa.Responses)
                                    .ToList();

                                var response = paResponses.FirstOrDefault(r => r.QuestionID == q.QuestionID);
                                var option = q.QuestionOptions.FirstOrDefault(o => o.OptionID == response?.QuestionOptionID);

                                return new QuestionUserAnswerDto
                                {
                                    UserID = uid,
                                    FullName = usersDict.TryGetValue(uid, out var name) ? name : "",
                                    Score = (int?)response?.Score,
                                    Justification = response?.Justification ?? "",
                                    OptionText = option?.OptionText ?? ""
                                };
                            }).ToDictionary(x=>x.UserID);

                            return new QuestionWithUserDto
                            {
                                QuestionID = q.QuestionID,
                                QuestionText = q.QuestionText,
                                DisplayOrder = q.DisplayOrder,
                                Users = userAnswers
                            };
                        }).ToList()
                }).ToList();

                return ResultResponseDto<List<PillarWithQuestionsDto>>.Success(result);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in GetPillarsWithQuestions", ex);
                return ResultResponseDto<List<PillarWithQuestionsDto>>.Failure(new[] { "There was an error, please try again later" });
            }
        }

        public async Task<Tuple<string, byte[]>> ExportPillarsHistoryByUserId(GetCityPillarHistoryRequestDto requestDto)
        {
            try
            {
                var response = await GetPillarsWithQuestions(requestDto);
                var city = await _context.Cities.FirstOrDefaultAsync(x => x.CityID == requestDto.CityID);

                if (!response.Succeeded)
                {
                    return new Tuple<string, byte[]>("", Array.Empty<byte>());
                }

                var byteArray = MakePillarSheet(response.Result, city);

                return new("ExportPillarsHistory"+ requestDto.CityID+""+requestDto.PillarID, byteArray);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in ExportPillarsHistoryByUserId", ex);
                return new Tuple<string, byte[]>("", Array.Empty<byte>());
            }
        }

        private byte[] MakePillarSheet(List<PillarWithQuestionsDto> pillars, Models.City? city)
        {
            using (var workbook = new XLWorkbook())
            {
                var name = city == null ? $"{pillars.Count}-Pillars-Result" : city?.CityName+"-"+city?.State+ $"-{pillars.Count}-Pillars-Result";
                var shortName = name.Length > 30 ? name.Substring(0, 30) : name;

                var ws = workbook.Worksheets.Add(shortName);
                ws.Columns().Width = 35;
                ws.Column(1).Width = 6;  // S.NO.
                ws.Column(2).Width = 100;  // Pillar/Question text

                var protection = ws.Protect();
                protection.AllowedElements =
                   XLSheetProtectionElements.FormatColumns |
                   XLSheetProtectionElements.SelectLockedCells |
                   XLSheetProtectionElements.SelectUnlockedCells;

                var names = pillars
                    .SelectMany(p => p.Questions)
                    .SelectMany(q => q.Users.Values)
                    .GroupBy(u => u.UserID)
                    .Select(g => g.First())
                    .ToList();

                int row = 1;
                int pillarCounter = 1;

                foreach (var pillar in pillars)
                {
                    int c = 1;

                    // Header row
                    ws.Cell(row, c++).Value = "S.NO.";
                    ws.Cell(row, c++).Value = "PillarName";
                    foreach (var user in names)
                        ws.Cell(row, c++).Value = user.FullName;

                    var headerRange = ws.Range(row, 1, row, names.Count + 2);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ++row;
                    c = 1;

                    // Pillar row
                    ws.Cell(row, c++).Value = pillarCounter++; // pillar serial number
                    ws.Cell(row, c++).Value = pillar.PillarName;
                    ws.Cell(row, 2).Style.Font.Bold = true;

                    foreach (var user in names)
                    {
                        var score = pillar.Questions
                            .SelectMany(x => x.Users)
                            .Where(x => x.Key == user.UserID)
                            .Sum(x => x.Value.Score) ?? 0;

                        var richText = ws.Cell(row, c++).GetRichText();

                        richText.AddText("Total Score:  ")
                            .SetBold().SetFontColor(XLColor.DarkGray);
                        richText.AddText($"{score}\n")
                            .SetFontColor(XLColor.Black);
                    }

                    row += 2;
                    c = 1;

                    // Question header row
                    ws.Cell(row, c++).Value = "S.NO.";
                    ws.Cell(row, c++).Value = "Questions";
                    foreach (var user in names)
                        ws.Cell(row, c++).Value = user.FullName;

                    var headerQRange = ws.Range(row, 1, row, names.Count + 2);
                    headerQRange.Style.Font.Bold = true;
                    headerQRange.Style.Fill.BackgroundColor = XLColor.TealBlue;
                    headerQRange.Style.Font.FontColor = XLColor.White;
                    headerQRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    var q = pillar.Questions;
                    int questionCounter = 1;

                    for (var i = 0; i < q.Count; i++)
                    {
                        ++row;
                        var question = q[i];
                        var usersData = question.Users;

                        c = 1;
                        ws.Cell(row, c++).Value = $"{pillarCounter - 1}.{questionCounter++}";
                        ws.Cell(row, 1).Style.Font.Bold = true;
                        ws.Cell(row, c++).Value = question.QuestionText;
    

                        foreach (var user in names)
                        {
                            usersData.TryGetValue(user.UserID, out var answerDto);
                            answerDto ??= new();

                            var richText = ws.Cell(row, c++).GetRichText();

                            richText.AddText("OptionText: ")
                               .SetBold().SetFontColor(XLColor.DarkRed);
                            richText.AddText($"{answerDto.OptionText ?? "-"}\n")
                                .SetFontColor(XLColor.Black);

                            richText.AddText("Score: ")
                                .SetBold().SetFontColor(XLColor.DarkBlue);
                            richText.AddText($"{answerDto.Score}\n")
                                .SetFontColor(XLColor.Black);

                            richText.AddText("Comment: ")
                                .SetBold().SetFontColor(XLColor.DarkGreen);
                            richText.AddText($"{answerDto.Justification ?? "-"}")
                                .SetFontColor(XLColor.Black);

                            ws.Cell(row, c - 1).Style.Alignment.WrapText = true;
                            ws.Row(row).Height = 60;
                        }
                    }

                    row += 2;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public async Task<ResultResponseDto<List<PillarsHistroyResponseDto>>> GetResponsesByUserId(
            GetPillarResponseHistoryRequestNewDto request,
            int userId,
            UserRole userRole)
        {
            try
            {
                var history = await _commonService
                    .GetUserProgressByAssessmentId(request.UserAssessmentMappingID);

                if (!history.Any())
                {
                    return ResultResponseDto<List<PillarsHistroyResponseDto>>
                        .Failure(new List<string> { "No data found for the given user and assessment." });
                }

                // Step 1: Get user pillars (DB query only once)
                var userPillars = await _context.UserPillarMappings
                    .Where(x => x.UserAssessmentMappingID == request.UserAssessmentMappingID &&
                               (x.UserID == userId ||
                               ((userRole == UserRole.Admin || userRole == UserRole.Executive) && x.User.Role == UserRole.Analyst))
                               && !x.IsDeleted && x.IsActive
                               )
                    .Select(x => new
                    {
                        x.PillarID,
                        x.Pillar.PillarName,
                        x.Pillar.DisplayOrder
                    })
                    .ToListAsync();

                // Step 2: Group history in memory (fast lookup)
                var groupedHistory = history
                    .GroupBy(x => x.PillarID)
                    .ToDictionary(
                        g => g.Key,
                        g => new PillarsHistroyResponseDto
                        {
                            PillarID = g.Key,
                            PillarName = g.First().PillarName,
                            DisplayOrder = g.First().DisplayOrder,
                            UserAssessmentMappingID = request.UserAssessmentMappingID,
                            Users = g.GroupBy(u => u.SubmittedByUserID)
                                     .Select(ug =>
                                     {
                                         var first = ug.First();
                                         return new PillarsUserHistroyResponseDto
                                         {
                                             UserID = ug.Key,
                                             FullName = first.SubmittedByUserName ?? "",
                                             ScoreProgress = first.ScoreProgress,
                                             TotalQuestion = first.TotalQuestions,
                                             AnsQuestion = first.TotalAns,
                                             CompeletionRate = first.CompletionRate
                                         };
                                     }).OrderBy(x=>x.UserID).ToList()
                        });

                // Step 3: Merge (Left Join equivalent, but cleaner)
                var result = userPillars    
                    .Select(p => groupedHistory.TryGetValue(p.PillarID, out var historyData)
                        ? historyData
                        : new PillarsHistroyResponseDto
                        {
                            PillarID = p.PillarID,
                            PillarName = p.PillarName,
                            DisplayOrder = p.DisplayOrder,
                            UserAssessmentMappingID = request.UserAssessmentMappingID,
                            Users = new List<PillarsUserHistroyResponseDto>()
                        })
                    .OrderBy(x => x.DisplayOrder)
                    .ToList();

                return ResultResponseDto<List<PillarsHistroyResponseDto>>
                    .Success(result, new List<string> { "Result found successfully." });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetPillarsHistoryByUserId", ex);

                return ResultResponseDto<List<PillarsHistroyResponseDto>>
                    .Failure(new List<string> { "Something went wrong while fetching data." });
            }
        }
    }
}