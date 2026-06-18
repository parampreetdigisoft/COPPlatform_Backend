using COPPlatform.Common.Interface;
using COPPlatform.Common.Models;
using COPPlatform.Common.Models.settings;
using COPPlatform.Data;
using COPPlatform.Dtos.CityDto;
using COPPlatform.Dtos.UserDtos;
using COPPlatform.IServices;
using COPPlatform.Models;
using COPPlatform.Views.EmailModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;

namespace COPPlatform.Services
{
    public class AuthService : IAuthService
    {
        #region  constructor
        private readonly ApplicationDbContext _context;
        private readonly AppSettings _appSettings;
        private readonly JwtSetting _jwtSetting;
        private readonly IEmailService _emailService;
        private readonly IAppLogger _appLogger;
        public AuthService(ApplicationDbContext context, IOptions<AppSettings> appSettings, IEmailService emailService, IOptions<JwtSetting> jwtSetting, IAppLogger appLogger)
        {
            _context = context;
            _appSettings = appSettings.Value;
            _emailService = emailService;
            _jwtSetting = jwtSetting.Value;
            _appLogger = appLogger;
        }
        #endregion

        #region IAuthService implemention

        public User Register(string fullName, string email, string phn, string password, UserRole role)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                FullName = fullName,
                Email = email,
                Phone = phn,
                PasswordHash = hash,
                Role = role,
                IsEmailConfirmed = false,
                Tier = role == UserRole.Executive ? Enums.TieredAccessPlan.Pending : null
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            return user;
        }
        public User GetByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }
        public async Task<User?> GetByEmailAysync(string email)
        {
            try
            {
                return await _context.Users.Where(u => u.Email == email && !u.IsDeleted).AsQueryable().FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("GetByEmailAysync", ex);
            }
            return null;
        }
        public bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        public async Task<ResultResponseDto<object>> ForgotPassword(string email)
        {
            try
            {
                var user = GetByEmail(email);
                if (user == null)
                {
                    return ResultResponseDto<object>.Failure(new string[] { "User not exist." });
                }
                else
                {
                    var hash = BCrypt.Net.BCrypt.HashPassword(email);
                    var passwordToken = hash;
                    var token = passwordToken.Replace("+", " ");

                    var url = _appSettings.ApplicationUrl;
                    string passwordResetLink = url + "/auth/reset-password?PasswordToken=" + token;

                    var sub = "Password Update Link – Grand Event Readiness System";
                    var model = new EmailInvitationSendRequestDto
                    {
                        ResetPasswordUrl = passwordResetLink,
                        Title = sub,
                        ApiUrl = _appSettings.ApiUrl,
                        ApplicationUrl = url,
                        MsgText= "A request was made to update the password for your Grand Event Readiness System account. To proceed, please use the secure link below:",
                        IsShowBtnText=true,
                        IsLoginBtn=false,
                        BtnText= "Update Password",
                        Mail=_appSettings.AdminMail,
                        DescriptionAboutBtnText = $"If you did not make this request, you may ignore this message and your account will remain unchanged."
                    };
                    var isMailSent = await _emailService.SendEmailAsync(email, sub, "~/Views/EmailTemplates/ChangePassword.cshtml", model);
                    if (isMailSent)
                    {
                        user.ResetToken = token;
                        user.ResetTokenDate = DateTime.UtcNow;
                        _context.Users.Update(user);
                        await _context.SaveChangesAsync();
                    }
                    return ResultResponseDto<object>.Success(new { }, new string[] { "Please check your email for change password." });

                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("ForgotPassword", ex);
                return ResultResponseDto<object>.Failure(new string[] { "There is an error please try later" });

            }
        }
        public async Task<ResultResponseDto<object>> ChangePassword(string passwordToken, string password)
        {
            try
            {
                var user = await _context.Users.Where(u => u.ResetToken == passwordToken).FirstOrDefaultAsync();

                if (user == null)
                {
                    return ResultResponseDto<object>.Failure(new string[] { "User not exist." });
                }
                if (_appSettings.LinkValidHours >= (DateTime.UtcNow - user.ResetTokenDate).Hours)
                {
                    var hash = BCrypt.Net.BCrypt.HashPassword(password);
                    user.PasswordHash = hash;
                    user.IsEmailConfirmed = true;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();

                    return ResultResponseDto<object>.Success(new { }, new string[] { "Password updated successfully" });
                }
                else
                {
                    return ResultResponseDto<object>.Failure(new string[] { "Link has been expired." });
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error change password", ex);
                return ResultResponseDto<object>.Failure(new string[] { "There is an error please try later" });
            }
        }
        public async Task<ResultResponseDto<UserResponseDto>> Login(string email, string password)
        {
            try
            {
                var user = await GetByEmailAysync(email);
                if (user == null || !VerifyPassword(password, user.PasswordHash))
                {
                    return ResultResponseDto<UserResponseDto>.Failure(new string[] { "Invalid request data." });
                }
                if (user.IsEmailConfirmed && !user.IsDeleted && user.Is2FAEnabled)
                {
                    var r = await SendTwoFactorOTPAsync(user);
                    if (r.Succeeded) 
                    {
                        var sendOpt = new UserResponseDto {};
                        return ResultResponseDto<UserResponseDto>.Success(sendOpt,
                          new string[] { "We've sent a one-time verification code (OTP) to your registered email address. Please check your inbox and enter the OTP to continue." });
                    }
                    return ResultResponseDto<UserResponseDto>.Failure(new string[] { "Faild to send OTP Please try again." });
                }
                else
                {
                    var response = GetAuthorizedUserDetails(user);
                    return response;
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error login", ex);
                return ResultResponseDto<UserResponseDto>.Failure(new string[] { ex.Message });
            }
        }
        public ResultResponseDto<UserResponseDto> GetAuthorizedUserDetails(User user)
        {
            if (user == null)
            {
                return ResultResponseDto<UserResponseDto>.Failure(new string[] { "Invalid request" });
            }
            if (!user.IsEmailConfirmed || user.IsDeleted)
            {
                string message = string.Empty;

                if (user.Role != UserRole.Executive)
                {
                    message = $"Your mail is not confirmed or de-activated by super {(user.Role == UserRole.Analyst ? "Admin" : "Analyst")}";
                }
                else
                {
                    message = "Your email is not verified. Please check your inbox and click the verification link. If the link has expired, you can reset your password to verify your account.";
                }

                return ResultResponseDto<UserResponseDto>.Failure(new string[] { message });
            }
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("Tier", user.Tier?.ToString() ?? ""),         
                new Claim("UserId", user!.UserID.ToString())       
            };
            var tokenExpired = DateTime.UtcNow.AddHours(1);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSetting.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var securityToken = new JwtSecurityToken(
                issuer: _jwtSetting.Issuer,
                audience: _jwtSetting.Audience,
                claims: claims,
                expires: tokenExpired,
                signingCredentials: creds
            );
            var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

            var response = new UserResponseDto
            {
                UserID = user.UserID,
                FullName = user.FullName,
                Phone = user.Phone,
                Email = user.Email,
                IsDeleted = user.IsDeleted,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                CreatedBy = user.CreatedBy,
                IsEmailConfirmed = user.IsEmailConfirmed,
                TokenExpirationDate = tokenExpired,
                ProfileImagePath = user.ProfileImagePath,
                Token = token,
                tier = user.Tier
            };
            return ResultResponseDto<UserResponseDto>.Success(response, new string[] { "You have successfully logged in." });
        }
        public async Task<ResultResponseDto<object>> AddStaffUser(RegisterDto inviteUser, UserRole userRole, int invitedUserId)
        {
            try
            {

                if (inviteUser == null ||
                    string.IsNullOrWhiteSpace(inviteUser.Email) ||
                    string.IsNullOrWhiteSpace(inviteUser.FullName))
                {
                    return ResultResponseDto<object>
                        .Failure(new[] { "Invalid request data." });
                }

                // Role permission rules
                if ((userRole == UserRole.Admin && inviteUser.Role == UserRole.Evaluator) ||
                    (userRole == UserRole.Analyst && inviteUser.Role != UserRole.Evaluator))
                {
                    return ResultResponseDto<object>
                        .Failure(new[] { "You are not authorized to assign this role." });
                }


                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Email == inviteUser.Email);

                bool isExistingUser = user != null;


                if (!isExistingUser)
                {
                    user = Register(
                        inviteUser.FullName,
                        inviteUser.Email,
                        inviteUser.Phone,
                        inviteUser.Password,
                        inviteUser.Role);

                    if (user == null)
                    {
                        return ResultResponseDto<object>
                            .Failure(new[] { "Failed to register user." });
                    }

                    user.CreatedBy = invitedUserId;
                }
                else
                {
                    if (user?.Role != inviteUser.Role)
                    {
                        return ResultResponseDto<object>
                            .Failure(new[] { "User already has a different role." });
                    }
                }

                user.FullName = inviteUser.FullName;
                user.Phone = inviteUser.Phone;
                user.IsDeleted = false;
                user.CreatedBy = invitedUserId;


                bool isMailSent = false;

                if (!user.IsEmailConfirmed)
                {
                    var token = BCrypt.Net.BCrypt
                        .HashPassword(user.Email)
                        .Replace("+", " ");

                    string subject =
                        $"{inviteUser.Role} Access Granted – Grand Event System";

                    string url = _appSettings.ApplicationUrl;

                    string resetLink =
                        $"{url}/auth/reset-password?PasswordToken={token}";

                    user.ResetToken = token;
                    user.ResetTokenDate = DateTime.UtcNow;

                    var model = new EmailInvitationSendRequestDto
                    {
                        ResetPasswordUrl = resetLink,
                        ApiUrl = _appSettings.ApiUrl,
                        Title = subject,
                        ApplicationUrl = url,
                        Mail = _appSettings.AdminMail
                    };

                    var viewPath = inviteUser.Role switch
                    {
                        UserRole.Analyst => "~/Views/EmailTemplates/AnalystSendInvitation.cshtml",
                        UserRole.Evaluator => "~/Views/EmailTemplates/EvaluatorSendInvitation.cshtml",
                        UserRole.Executive => "~/Views/EmailTemplates/ExecutiveSendInvitation.cshtml",
                        _ => "",
                    };
                    if (viewPath == "")
                    {
                        return ResultResponseDto<object>.Failure(new[] { $"Invalid role for user {inviteUser.Email}." });
                    }

                    isMailSent = await _emailService
                        .SendEmailAsync(user.Email, subject, viewPath, model);
                }

                await _context.SaveChangesAsync();

                string msg;

                if (isExistingUser)
                {
                    msg = isMailSent
                        ? "User already exists. Invitation email sent."
                        : "User updated successfully.";
                }
                else
                {
                    msg = isMailSent
                        ? "User created successfully. Invitation email sent."
                        : "User created successfully.";
                }

                return ResultResponseDto<object>
                    .Success(new { }, new[] { msg });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in AddStaffUser", ex);

                return ResultResponseDto<object>
                    .Failure(new[] { "There is an error. Please try later." });
            }
        }
        public async Task<ResultResponseDto<object>> DeleteUser(int deleteUserId, UserRole userRole, int userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(m => m.UserID == deleteUserId && !m.IsDeleted);

                if (user == null || (userRole == UserRole.Analyst && user.CreatedBy != userId))
                {
                    return ResultResponseDto<object>.Failure(new string[] { "User does not exist or unauthorized action" });
                }

                user.IsDeleted = true;
                _context.Users.Update(user);

                if (userRole == UserRole.Admin)
                {
                    var userMapping = _context.UserAssessmentMappings.Include(x => x.UserPillarMappings).Where(x => x.UserID == deleteUserId).ToList();
                    foreach (var m in userMapping)
                    {
                        m.IsDeleted = true;
                        m.IsActive = false;
                        _context.UserAssessmentMappings.Update(m);

                        if (m.UserPillarMappings != null && m.UserPillarMappings.Count > 0)
                        {
                            foreach (var p in m.UserPillarMappings)
                            {
                                p.IsDeleted = true;
                                p.IsActive = false;
                                _context.UserPillarMappings.Update(p);
                            }
                        }
                    }
                }
                else if (userRole == UserRole.Analyst)
                {
                    Expression<Func<UserPillarMapping, bool>> pillarFilter = m =>
                        m.UserID == deleteUserId &&
                        m.AssignedByUserId == userId &&
                        !m.IsDeleted &&
                        m.IsActive;

                    var pillarMapping = _context.UserPillarMappings.Where(pillarFilter).ToList();
                    foreach (var p in pillarMapping)
                    {
                        p.IsDeleted = true;
                        p.IsActive = false;
                        _context.UserPillarMappings.Update(p);
                    }
                }

                await _context.SaveChangesAsync();

                return ResultResponseDto<object>.Success(new { }, new string[] { "User deleted successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in DeleteUser", ex);
                return ResultResponseDto<object>.Failure(new string[] { "There is an error please try later" });
            }
        }
        public async Task<ResultResponseDto<UserResponseDto>> RefreshToken(int userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(x => !x.IsDeleted && x.UserID == userId);
                if (user == null)
                {
                    return ResultResponseDto<UserResponseDto>.Failure(new string[] { "Invalid request data." });
                }
                var response = GetAuthorizedUserDetails(user);

                return await Task.FromResult(response);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure RefreshToken", ex);
                return ResultResponseDto<UserResponseDto>.Failure(new string[] { "There is an error please try later" });
            }
        }


        public async Task<ResultResponseDto<object>> ConfirmMail(string passwordToken)
        {
            try
            {
                var user = await _context.Users.Where(u => u.ResetToken == passwordToken).FirstOrDefaultAsync();

                if (user == null)
                {
                    return ResultResponseDto<object>.Failure(new string[] { "User not exist." });
                }
                if (_appSettings.LinkValidHours >= (DateTime.UtcNow - user.ResetTokenDate).Hours)
                {
                    user.IsEmailConfirmed = true;
                    if (!string.IsNullOrEmpty(user.TemporaryMail))
                    {
                        user.Email = user.TemporaryMail;
                        user.TemporaryMail = null;

                        _context.Users.Update(user);
                        await _context.SaveChangesAsync();
                    }

                    return ResultResponseDto<object>.Success(new { }, new string[] { "Mail Confirmed Successfully, You Can Login Now!" });
                }
                else
                {
                    return ResultResponseDto<object>.Failure(new string[] { "Link has been expired. You can reset your password" });
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error change password", ex);
                return ResultResponseDto<object>.Failure(new string[] { "There is an error please try later" });
            }
        }
        public async Task<ResultResponseDto<object>> ContactUs(ContactUsRequestDto requestDto)
        {
            try
            {
                var emailModel = new EmailInvitationSendRequestDto
                {
                    ResetPasswordUrl = "",
                    Title = $"{requestDto.Subject} - {requestDto.Email}",
                    ApiUrl = _appSettings.ApiUrl,
                    ApplicationUrl = _appSettings.ApplicationUrl,
                    MsgText = requestDto.Message,
                    DescriptionAboutBtnText
                        = $"This email was sent by {requestDto.Name} from {requestDto.City}, {requestDto.Country}. You can reach them at: {requestDto.Email}.",
                    IsLoginBtn = false,
                    IsShowBtnText = false,
                    Mail = _appSettings.AdminMail
                };

                var isMailSend = await _emailService.SendEmailAsync(
                    _appSettings.ApplicationInfoMail,
                    requestDto.Subject,
                    "~/Views/EmailTemplates/ChangePassword.cshtml",
                    emailModel
                );

                if (isMailSend)
                {
                    return ResultResponseDto<object>.Success(
                        new { },
                        new string[] { "Thank you for contacting us. Our team will reach out to you shortly." }
                    );
                }
                else
                {
                    return ResultResponseDto<object>.Failure(new string[] { "Unable to send your message at the moment. Please try again later." });
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in ContactUs", ex);
                return ResultResponseDto<object>.Failure(
                    new string[] { "An unexpected error occurred. Please try again later." }
                );
            }
        }
        public async Task<ResultResponseDto<string>> SendTwoFactorOTPAsync(User user)
        {
            try
            {
                // 1️⃣ Generate secure random 6-digit OTP
                var random = new Random();
                var otp = random.Next(100000, 999999).ToString();

                // 3️⃣ Store hashed OTP + expiry
                user.ResetToken = otp;
                user.ResetTokenDate = DateTime.UtcNow; 

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                var url = _appSettings.ApplicationUrl;
                // 4️⃣ Send the OTP via email
                var model = new EmailInvitationSendRequestDto
                {
                    Title = "Two-Factor Authentication (2FA) Code",
                    ApiUrl = _appSettings.ApiUrl,
                    ApplicationUrl = url,
                    MsgText = $"Your one-time password (OTP) for login verification is ( {otp} ). " +
                               $"This code will expire in {_appSettings.OTPExpiryValidMinutes} minutes. " +
                               $"Please do not share this code with anyone.",
                    IsLoginBtn = false,
                    IsShowBtnText = false,
                    Mail = _appSettings.AdminMail,
                    DescriptionAboutBtnText = "You are receiving this email because a login attempt was made to your PEM account. " +
                               "If this was you, please use the above OTP to complete your sign-in. " +
                               "If you did not request this login, please secure your account immediately by resetting your password."
                };

                var isMailSent = await _emailService.SendEmailAsync(
                    user.Email,
                    "Your 2FA Verification Code",
                    "~/Views/EmailTemplates/ChangePassword.cshtml",
                    model
                );

                if (!isMailSent)
                    return ResultResponseDto<string>.Failure(new[] { "Failed to send OTP. Please try again." });

                return ResultResponseDto<string>.Success("", new[] { "OTP sent successfully to your email." });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in SendTwoFactorOTPAsync", ex);
                return ResultResponseDto<string>.Failure(new[] { "There was an error while sending OTP. Please try again later." });
            }
        }
        public async Task<ResultResponseDto<UserResponseDto>> TwofaVerification(string email, int otp)
        {
            try
            {
                var user = await GetByEmailAysync(email);
                if (user == null)
                    return ResultResponseDto<UserResponseDto>.Failure(new[] { "User not found. Please check your email and try again." });

                if (string.IsNullOrEmpty(user.ResetToken) || !int.TryParse(user.ResetToken, out var existingOtp))
                    return ResultResponseDto<UserResponseDto>.Failure(new[] { "Invalid or missing OTP. Please request a new one." });

                if (existingOtp != otp)
                    return ResultResponseDto<UserResponseDto>.Failure(new[] { "Incorrect OTP. Please verify and try again." });

                var timeElapsed = (DateTime.UtcNow - user.ResetTokenDate).TotalMinutes;
                if (timeElapsed > _appSettings.OTPExpiryValidMinutes)
                    return ResultResponseDto<UserResponseDto>.Failure(new[] { "OTP has expired. Please request a new one." });

                var response = GetAuthorizedUserDetails(user);
                return response;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error during 2FA verification", ex);
                return ResultResponseDto<UserResponseDto>.Failure(new[] { "An unexpected error occurred. Please try again later." });
            }
        }
        public async Task<ResultResponseDto<string>> ReSendLoginOtp(string email)
        {
            try
            {
                var user = await GetByEmailAysync(email);
                if (user == null)
                    return ResultResponseDto<string>.Failure(new[] { "User not found. Please check your email and try again." });
                return await SendTwoFactorOTPAsync(user);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in SendTwoFactorOTPAsync", ex);
                return ResultResponseDto<string>.Failure(new[] { "There was an error while sending OTP. Please try again later." });
            }
        }
        public async Task<ResultResponseDto<string>> AddUpdateInvitation(UpdateInviteUserDto inviteUser, UserRole userRole, int invitedUserID)
        {
            try
            {
                if (inviteUser == null || (userRole == UserRole.Analyst && (inviteUser.UserAssessmentMappingID == null || inviteUser.UserAssessmentMappingID == 0)))
                    return ResultResponseDto<string>.Failure(new[] { "Invalid request." });

                string msg = "Invitation updated successfully"; ;
                UserAssessmentMapping? mapping;

               bool isNewInvitation = false;

                #region ADD UPDATE

                if (inviteUser.UserAssessmentMappingID.HasValue)
                {
                    mapping = await _context.UserAssessmentMappings
                        .Include(x => x.UserPillarMappings)
                        .FirstOrDefaultAsync(m =>
                            m.UserAssessmentMappingID == inviteUser.UserAssessmentMappingID.Value &&
                            !m.IsDeleted &&
                            m.IsActive);
                    if (mapping == null)
                    {
                        return ResultResponseDto<string>.Failure(new[] { "Invalid request." });
                    }
                }
                else
                {                    
                    mapping = await _context.UserAssessmentMappings
                        .Include(x => x.UserPillarMappings)
                        .FirstOrDefaultAsync(m =>
                            m.UserID == inviteUser.UserID &&
                            m.Year == inviteUser.Year &&
                            m.AssignedByUserId == invitedUserID &&
                            !m.IsDeleted &&
                            m.IsActive);

                    if(mapping != null)
                    {
                        return ResultResponseDto<string>.Failure( new[] { "Assessment already exists for this year. Please update it instead." });
                    }
                    else if(userRole== UserRole.Admin)
                    {
                        mapping = new UserAssessmentMapping
                        {
                            UserID = inviteUser.UserID,
                            Year = inviteUser.Year,
                            GeographicReference = inviteUser.GeographicReference,
                            DueDate = inviteUser.DueDate,
                            AssignedByUserId = invitedUserID,
                            IsActive = true,
                            IsDeleted = false,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.UserAssessmentMappings.Add(mapping);

                        isNewInvitation = true;
                        await _context.SaveChangesAsync();
                        msg = "Invitation created successfully";
                    }
                    else
                    {
                       return ResultResponseDto<string>.Failure(new[] { "No existing assessment found to update, and only Admin can create new invitations." });
                    }                  
                }
              
                if(mapping !=null && userRole != UserRole.Analyst)
                {
                    mapping.GeographicReference = inviteUser.GeographicReference;
                    mapping.DueDate = inviteUser.DueDate;
                    mapping.IsActive = true;
                    mapping.IsDeleted = false;
                    mapping.UpdatedAt = DateTime.UtcNow;
                }
                #endregion


                #region PILLAR MAPPING

                Expression<Func<UserPillarMapping, bool>> pillarFilter = m =>
                    m.UserID == inviteUser.UserID &&
                    m.Year == inviteUser.Year &&
                    m.AssignedByUserId == invitedUserID &&
                    !m.IsDeleted &&
                    m.IsActive;

                var existingPillars = mapping?.UserPillarMappings?.Where(pillarFilter.Compile()) ?? new List<UserPillarMapping>();
                var newPillarIds = inviteUser.PillarIDs ?? new List<int>();

                

                // Add or Update
                foreach (var pillarID in newPillarIds)
                {
                    var pillar = existingPillars.Where(m=> m.PillarID == pillarID).FirstOrDefault();

                    if (pillar != null)
                    {
                        pillar.DueDate = inviteUser.DueDate;
                        pillar.IsActive = true;
                        pillar.IsDeleted = false;
                        pillar.UpdatedAt = DateTime.UtcNow;
                        msg = "Invitation updated successfully";
                    }
                    else
                    {
                        _context.UserPillarMappings.Add(new UserPillarMapping
                        {
                            UserAssessmentMappingID = mapping.UserAssessmentMappingID,
                            UserID = inviteUser.UserID,
                            Year = inviteUser.Year,
                            PillarID = pillarID,
                            DueDate = inviteUser.DueDate,
                            AssignedByUserId = invitedUserID,
                            IsActive = true,
                            IsDeleted = false,
                            UpdatedAt = DateTime.UtcNow
                        });
                        msg = "Invitation created successfully";
                        isNewInvitation = userRole == UserRole.Analyst  ? true : isNewInvitation;//admin add analyst
                    }
                }

                // Soft delete removed pillars
                var pillarsToRemove = existingPillars
                    .Where(x => !newPillarIds.Contains(x.PillarID) && !x.IsDeleted)
                    .ToList();

                foreach (var pillar in pillarsToRemove)
                {
                    pillar.IsActive = false;
                    pillar.IsDeleted = true;
                    pillar.UpdatedAt = DateTime.UtcNow;
                }

                #endregion

                await _context.SaveChangesAsync();
                if (mapping !=null)
                {
                   await SendAssessmentMail(mapping, userRole, inviteUser.UserID, isNewInvitation);
                }
                

                return ResultResponseDto<string>.Success(msg, new[] { msg });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in AddUpdateInvitation", ex);
                return ResultResponseDto<string>.Failure(
                    new[] { "There is an error. Please try later." });
            }
        }

        public async Task<bool> SendAssessmentMail(UserAssessmentMapping inviteUser, UserRole userRole, int userID, bool isNewInvitation)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(x => x.UserID == userID);
                if (user == null)
                {
                    return false;
                }
                bool isMailSent = false;

                string subject = isNewInvitation
                 ? "New Assessment Invitation – Grand Event System"
                 : "New Pillar Assignment Added – Grand Event System";

                string url = _appSettings.ApplicationUrl;

                string resetLink = string.Empty; 

                if (user.Role == UserRole.Analyst)
                {
                   resetLink = $"{url}/analyst/analyst-assessment?userAssessmentMappingID={inviteUser.UserAssessmentMappingID}";
                }
                else
                {
                    resetLink = $"{url}/evaluator/make-assessment?userAssessmentMappingID={inviteUser.UserAssessmentMappingID}";
                }

                var model = new EmailInvitationSendRequestDto
                {
                    ResetPasswordUrl = resetLink,
                    ApiUrl = _appSettings.ApiUrl,
                    Title = subject,
                    ApplicationUrl = url,
                    Mail = _appSettings.AdminMail,
                    UserName = user.FullName,
                    Role = user.Role
                };

                var viewPath = "~/Views/EmailTemplates/SendAssessmentTemplate.cshtml";


                isMailSent = await _emailService
                    .SendEmailAsync(user.Email, subject, viewPath, model);

                return isMailSent;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in SendAssessmentMail", ex);
                return false;
            }

        }
        public async Task<ResultResponseDto<object>> InviteBulkUser(InviteBulkUserDto inviteUserList, UserRole userRole, int invitedUserID)
        {
            try
            {
                if (inviteUserList?.users == null || !inviteUserList.users.Any())
                    return ResultResponseDto<object>
                        .Failure(new[] { "No users provided." });

                var emails = inviteUserList.users
                    .Select(u => u.Email?.Trim().ToLower())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct()
                    .ToList();

                var existingUsers = await _context.Users
                    .Where(u => emails.Contains(u.Email.ToLower()))
                    .ToDictionaryAsync(u => u.Email.ToLower());

                var emailTasks = new List<Task>();
                var errors = new List<string>();

                foreach (var inviteUser in inviteUserList.users)
                {
                    if (inviteUser == null ||
                        string.IsNullOrWhiteSpace(inviteUser.Email) ||
                        string.IsNullOrWhiteSpace(inviteUser.FullName))
                    {
                        errors.Add("Invalid user data.");
                        continue;
                    }

                    var emailKey = inviteUser.Email.Trim().ToLower();

                    existingUsers.TryGetValue(emailKey, out var user);

                    if (user == null)
                    {
                        user = new User
                        {
                            FullName = inviteUser.FullName,
                            Email = inviteUser.Email,
                            Phone = inviteUser.Phone,
                            PasswordHash = BCrypt.Net.BCrypt
                                .HashPassword(inviteUser.Password),
                            Role = inviteUser.Role,
                            CreatedBy = invitedUserID,
                            IsDeleted = false
                        };

                        _context.Users.Add(user);
                        existingUsers[emailKey] = user;
                    }
                    else
                    {
                        // Role mismatch check
                        if (user.Role != inviteUser.Role)
                        {
                            errors.Add(
                                $"User {inviteUser.Email} already has a different role.");
                            continue;
                        }

                        // Update existing user
                        user.FullName = inviteUser.FullName;
                        user.Phone = inviteUser.Phone;
                        user.IsDeleted = false;
                    }

                    if (!user.IsEmailConfirmed)
                    {
                        var token = BCrypt.Net.BCrypt
                            .HashPassword(inviteUser.Email)
                            .Replace("+", " ");

                        var url = _appSettings.ApplicationUrl;

                        string resetLink =
                            $"{url}/auth/reset-password?PasswordToken={token}";

                        string subject =
                            $"{inviteUser.Role} Access Granted – Grand Event Readiness System";

                        var model = new EmailInvitationSendRequestDto
                        {
                            ResetPasswordUrl = resetLink,
                            ApiUrl = _appSettings.ApiUrl,
                            ApplicationUrl = url,
                            Title = subject,
                            Mail = _appSettings.AdminMail
                        };

                        var viewPath = inviteUser.Role switch
                        {
                            UserRole.Analyst => "~/Views/EmailTemplates/AnalystSendInvitation.cshtml",
                            UserRole.Evaluator => "~/Views/EmailTemplates/EvaluatorSendInvitation.cshtml",
                            UserRole.Executive => "~/Views/EmailTemplates/ExecutiveSendInvitation.cshtml",
                            _ =>"",
                        };
                        if(viewPath == "")
                        {
                            errors.Add($"Invalid role for user {inviteUser.Email}.");
                            continue;
                        }
                      

                        emailTasks.Add(_emailService.SendEmailAsync(inviteUser.Email, subject, viewPath, model));

                        user.ResetToken = token;
                        user.ResetTokenDate = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                if (emailTasks.Any())
                    await Task.WhenAll(emailTasks);

                string msg = errors.Any()
                    ? "Bulk invite completed with some issues."
                    : "Bulk invite completed successfully.";

                return ResultResponseDto<object>
                    .Success(new { Errors = errors }, new[] { msg });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Invite Bulk User", ex);

                return ResultResponseDto<object>
                    .Failure(new[] { "Bulk invitation failed." });
            }
        }


        #endregion
    }
}
