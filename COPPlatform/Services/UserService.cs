using COPPlatform.Common.Implementation;
using COPPlatform.Common.Models;
using COPPlatform.Data;
using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.CityDto;
using COPPlatform.Dtos.CommonDto;
using COPPlatform.Dtos.PublicDto;
using COPPlatform.Dtos.UserDtos;
using COPPlatform.IServices;
using COPPlatform.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace COPPlatform.Services
{
    public class UserService : IUserService
    {
        private readonly IAppLogger _appLogger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public UserService(ApplicationDbContext context, IAppLogger appLogger, IWebHostEnvironment env)
        {
            _context = context;
            _appLogger = appLogger;
            _env = env;
        }
        public User GetByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }
        public async Task<PaginationResponse<GetUserByRoleResponse>> GetUserByRoleWithAssignedCity(GetUserByRoleRequestDto request, UserRole userRole)
        {
            try
            {                
                var filteredMappings =
                    _context.UserAssessmentMappings
                        .Where(x => !x.IsDeleted &&
                               (x.AssignedByUserId == request.UserID || userRole == UserRole.Admin)&& x.Year == request.Year);

                Expression<Func<User, bool>> predicate = userRole switch
                {
                    UserRole.Admin => x => !x.IsDeleted && request.GetUserRole.HasValue ? x.Role == request.GetUserRole : (x.Role == UserRole.Evaluator || x.Role == UserRole.Executive),
                    _ => x => !x.IsDeleted && x.Role == UserRole.Evaluator && x.CreatedBy == request.UserId
                };

                // Build one-row-per-user by taking at most 1 mapping row per user
                // NOTE: use a deterministic column to order (e.g., CreatedAt or primary key).
                var query =
                    from u in _context.Users.Where(predicate)
                    from ab in _context.Users
                                .Where(p => p.UserID == u.CreatedBy)
                                .DefaultIfEmpty()
                    select new GetUserByRoleResponse
                    {
                        UserID = u.UserID,
                        FullName = u.FullName,
                        Email = u.Email,
                        Phone = u.Phone,
                        Role = u.Role.ToString(),
                        CreatedBy = u.CreatedBy,
                        IsDeleted = u.IsDeleted,
                        IsEmailConfirmed = u.IsEmailConfirmed,
                        CreatedAt = u.CreatedAt,
                        CreatedByName = ab != null ? ab.FullName : null
                    };


                // Apply pagination + search
                var response = await query.ApplyPaginationAsync(
                    request,
                    x => string.IsNullOrEmpty(request.SearchText) ||
                         x.Email.Contains(request.SearchText) ||
                         x.FullName.Contains(request.SearchText));
                
                return response;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in GetUserByRoleWithAssignedCity", ex);
                return new PaginationResponse<GetUserByRoleResponse>();
            }
        }
        public async Task<ResultResponseDto<List<PublicUserResponse>>> GetAccessUsers(GetAssignUserDto request, UserRole userRole, int userId)
        {
            try
            {
                IQueryable<PublicUserResponse> query;

                if(userRole == UserRole.Admin)
                {
                    query =
                    from u in _context.Users.Where(x => !x.IsDeleted && x.Role == request.UserRole)
                    select new PublicUserResponse
                    {
                        UserID = u.UserID,
                        FullName = u.FullName,
                        Email = u.Email,
                        Phone = u.Phone,
                        Role = u.Role.ToString(),
                        IsDeleted = u.IsDeleted,
                        IsEmailConfirmed = u.IsEmailConfirmed,
                        CreatedAt = u.CreatedAt
                    };
                }
                else
                {
                    query =
                    from  u in _context.Users
                        .Where(x => !x.IsDeleted && x.CreatedBy == userId && x.Role == UserRole.Evaluator)
                    select new PublicUserResponse
                    {
                        UserID = u.UserID,
                        FullName = u.FullName,
                        Email = u.Email,
                        Phone = u.Phone,
                        Role = u.Role.ToString(),
                        IsDeleted = u.IsDeleted,
                        IsEmailConfirmed = u.IsEmailConfirmed,
                        CreatedAt = u.CreatedAt
                    };
                }

                var users = await query
                       .Distinct()
                       .OrderBy(x => x.FullName)
                       .ToListAsync();

                return ResultResponseDto<List<PublicUserResponse>>
                    .Success(users, new[] { "User fetched successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetAccessUsers", ex);
                return ResultResponseDto<List<PublicUserResponse>>
                    .Failure(new[] { "There is an error, please try later" });
            }
        }


        public async Task<ResultResponseDto<UpdateUserResponseDto>> UpdateUser(UpdateUserDto requestDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(requestDto.UserID);
                if (user == null)
                    return ResultResponseDto<UpdateUserResponseDto>.Failure(new List<string>() { "Invalid request " });

                // Update fields
                user.FullName = requestDto.FullName;
                user.Phone = requestDto.Phone;
                user.Is2FAEnabled = requestDto.Is2FAEnabled;
                // Handle profile image upload
                if (requestDto.ProfileImage != null)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    // ?? Remove old image if exists
                    if (!string.IsNullOrEmpty(user.ProfileImagePath))
                    {
                        string oldFilePath = Path.Combine(_env.WebRootPath, user.ProfileImagePath.TrimStart('/'));
                        if (File.Exists(oldFilePath))
                        {
                            File.Delete(oldFilePath);
                        }
                    }

                    // Save new image
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(requestDto.ProfileImage.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await requestDto.ProfileImage.CopyToAsync(stream);
                    }

                    user.ProfileImagePath = "/uploads/" + fileName;
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                var response = new UpdateUserResponseDto
                {
                    UserID = user.UserID,
                    FullName = user.FullName,
                    Phone = user.Phone,
                    Is2FAEnabled = user.Is2FAEnabled,
                    ProfileImagePath = user.ProfileImagePath,
                    Tier = user.Tier ?? Enums.TieredAccessPlan.Pending
                };

                return ResultResponseDto<UpdateUserResponseDto>.Success(response, new List<string> { "Updated successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure UpdateUser", ex);
                return ResultResponseDto<UpdateUserResponseDto>.Failure(new string[] { "There is an error please try later" });
            }
        }
        public async Task<ResultResponseDto<List<GetAssessmentResponseDto>>> GetUsersAssignedToCity(int cityId)
        {
            try
            {
                var year = DateTime.Now.Year;
                var query =
                from u in _context.Users
                where !u.IsDeleted
                join uc in _context.UserAssessmentMappings
                        .Where(x => !x.IsDeleted && x.CityID == cityId)
                    on u.UserID equals uc.UserID
                join c in _context.Cities.Where(x => !x.IsDeleted)
                    on uc.CityID equals c.CityID
                join createdBy in _context.Users.Where(x => !x.IsDeleted)
                    on uc.AssignedByUserId equals createdBy.UserID into createdByUser
                from createdBy in createdByUser.DefaultIfEmpty()

                    // LEFT JOIN to Assessments
                join a in _context.Assessments
                        .Include(q => q.PillarAssessments)
                            .ThenInclude(q => q.Responses).Where(x=>x.IsActive && x.CreatedAt.Year == year)
                    on uc.UserAssessmentMappingID equals a.UserAssessmentMappingID into userAssessment
                from a in userAssessment.DefaultIfEmpty()

                select new GetAssessmentResponseDto
                {
                    AssessmentID = a != null ? a.AssessmentID : 0,
                    UserAssessmentMappingID = uc.UserAssessmentMappingID,
                    CreatedAt = a != null ? a.CreatedAt : null,
                    CityID = c.CityID,
                    CityName = c.CityName,
                    State = c.State,
                    UserID = u.UserID,
                    UserName = u.FullName,
                    Score = a != null
                        ? a.PillarAssessments.SelectMany(x => x.Responses)
                            .Where(r => r.Score.HasValue && (int)r.Score.Value <= (int)ScoreValue.Four)
                            .Sum(r => (int?)r.Score ?? 0)
                        : 0,
                    AssignedByUser = createdBy != null ? createdBy.FullName : "",
                    AssignedByUserId = createdBy != null ? createdBy.UserID : 0,
                    AssessmentYear = a != null ? a.UpdatedAt.Year : 0,
                    AssessmentPhase = a != null ? a.AssessmentPhase : null
                };



                var users = await query.Distinct().ToListAsync();

                return ResultResponseDto<List<GetAssessmentResponseDto>>.Success(users, new[] { "user get successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in GetUsersAssignedToCity", ex);
                return ResultResponseDto<List<GetAssessmentResponseDto>>.Failure(new string[] { "There is an error please try later" });
            }
        }
        public async Task<ResultResponseDto<UpdateUserResponseDto>> GetUserInfo(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return ResultResponseDto<UpdateUserResponseDto>.Failure(new List<string>() { "Invalid request " });

                var response = new UpdateUserResponseDto
                {
                    UserID = user.UserID,
                    FullName = user.FullName,
                    Phone = user.Phone,
                    Email = user.Email,
                    ProfileImagePath = user?.ProfileImagePath,
                    Is2FAEnabled = user?.Is2FAEnabled ?? false,
                    Tier = user?.Tier ?? Enums.TieredAccessPlan.Pending
                };

                return ResultResponseDto<UpdateUserResponseDto>.Success(response, new List<string> { "Updated successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure UpdateUser", ex);
                return ResultResponseDto<UpdateUserResponseDto>.Failure(new string[] { "There is an error please try later" });
            }
        }

        public async Task<PaginationResponse<GetInvitationResponseDto>> GetInvitations(GetInvitationRequestDto request,UserRole userRole,int userId)
        {
            try
            {

                Expression<Func<UserPillarMapping, bool>> pillarFilter = userRole switch
                {
                    UserRole.Evaluator => x => x.AssignedByUserId == userId && !x.IsDeleted && x.IsActive && (!request.Year.HasValue || x.Year == request.Year),

                    UserRole.Analyst => x => !x.IsDeleted && x.IsActive 
                                && (!request.Year.HasValue || x.Year == request.Year) && x.User.Role == request.GetUserRole && 
                                (request.GetUserRole == UserRole.Analyst ? x.UserID == userId: x.AssignedByUserId == userId),

                    UserRole.Admin => x => !x.IsDeleted && x.IsActive && (!request.Year.HasValue || x.Year == request.Year) && x.User.Role == request.GetUserRole,
                    _ => x => false

                };


                var filteredMappings = _context.UserPillarMappings.Where(pillarFilter);


                // ? STEP 1 — GROUP ONLY BY CORE KEYS
                var baseQuery =
                    from uc in filteredMappings
                    group uc by new
                    {
                        uc.UserAssessmentMappingID,
                        uc.Year,
                        uc.UserID
                    }
                    into g

                    select new GetInvitationResponseDto
                    {
                        UserAssessmentMappingID = g.Key.UserAssessmentMappingID,
                        Year = g.Key.Year,
                        UserID = g.Key.UserID,
                        GeographicReference = g.Select(x => x.UserAssessmentMapping.GeographicReference).FirstOrDefault() ?? "",
                        DueDate = g.Max(x => x.DueDate),
                        UpdatedAt = g.Max(x => x.UpdatedAt),
                        NumOfUser = g.Where(x=>x.IsActive && !x.IsDeleted).Select(x=>x.UserID).Distinct().Count()
                    };

                // ? STEP 2 — PAGINATION + SEARCH
                var response = await baseQuery.ApplyPaginationAsync(
                    request,
                    x => string.IsNullOrEmpty(request.SearchText) ||
                         x.Email.Contains(request.SearchText) ||
                         x.FullName.Contains(request.SearchText)
                );

                // ? STEP 3 — LOAD CHILD DATA FOR PAGE ONLY
                var mappingIds = response.Data
                    .Select(x => x.UserAssessmentMappingID)
                    .ToList();

                if (mappingIds.Count > 0)
                {
                    var pillarData = await _context.UserPillarMappings.Where(pillarFilter)
                        .Where(x => mappingIds.Contains(x.UserAssessmentMappingID))
                        .Select(x => new
                        {
                            x.UserAssessmentMappingID,
                            x.User,
                            Pillar = new InvitationPillarResponseDto
                            {
                                UserPillarMappingID = x.UserPillarMappingID,
                                PillarID = x.PillarID,
                                PillarName = x.Pillar.PillarName,
                                ImagePath = x.Pillar.ImagePath,
                                DisplayOrder = x.Pillar.DisplayOrder
                            }
                        })
                        .ToListAsync();

                    // ? STEP 4 — COMBINE RESULTS
                    foreach (var item in response.Data)
                    {
                        var assessments = pillarData
                            .Where(x => x.UserAssessmentMappingID == item.UserAssessmentMappingID);
                        
                        var user = assessments?.Select(x => x.User)?.FirstOrDefault(x=>x?.UserID == item.UserID);
                        if (user != null)
                        {
                            item.Email = user.Email;
                            item.FullName = user.FullName;
                            item.Role = user.Role.ToString();
                        }

                        item.Pillars = assessments
                            .Select(x => x.Pillar)
                            .OrderBy(p => p.DisplayOrder)
                            .ToList();
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetInvitations", ex);
                return new PaginationResponse<GetInvitationResponseDto>();
            }
        }

        public async Task<ResultResponseDto<string>> DeleteInvitation(DeleteInvitationDto request, UserRole userRole, int userId)
        {
            try
            {
                var userMapping = await _context.UserAssessmentMappings
                                                .Include(x => x.UserPillarMappings)
                                                .Where(x => x.UserAssessmentMappingID == request.UserAssessmentMappingID)
                                                .FirstOrDefaultAsync();

                if (userMapping == null)
                {
                    return ResultResponseDto<string>.Failure(new string[] { "User assessment mapping not found." });
                }

                if (userRole == UserRole.Admin)
                {
                    userMapping.IsDeleted = true;
                    userMapping.IsActive = false;
                    _context.UserAssessmentMappings.Update(userMapping);
                }

                if (userMapping.UserPillarMappings != null && userMapping.UserPillarMappings.Count > 0)
                {
                    var deleteMappings = userMapping.UserPillarMappings
                                                    .Where(x => x.UserID == request.UserID && (x.AssignedByUserId == userId || userRole == UserRole.Admin))
                                                    .ToList();

                    foreach (var p in deleteMappings)
                    {
                        p.IsDeleted = true;
                        p.IsActive = false;
                        _context.UserPillarMappings.Update(p);
                    }
                }
                await _context.SaveChangesAsync();

                return ResultResponseDto<string>.Success("", new string[] { "Invitation deleted successfully." });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in DeleteInvitation", ex);
                return ResultResponseDto<string>.Failure(new string[] { "An error occurred. Please try again later." });
            }
        }
    }
}