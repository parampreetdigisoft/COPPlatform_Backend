using COPPlatform.Common.Models;
using COPPlatform.Dtos.CityDto;
using COPPlatform.Dtos.UserDtos;
using COPPlatform.Models;

namespace COPPlatform.IServices
{
    public interface IAuthService
    {
        User Register(string fullName, string email, string phn, string password, UserRole role);
        User GetByEmail(string email);
        Task<User?> GetByEmailAysync(string email);
        bool VerifyPassword(string password, string hash);
        Task<ResultResponseDto<UserResponseDto>> Login(string email, string password);
        Task<ResultResponseDto<object>> ForgotPassword(string email);
        Task<ResultResponseDto<object>> ChangePassword(string passwordToken, string password);
        Task<ResultResponseDto<object>> AddStaffUser(RegisterDto inviteUser, UserRole userRole, int userId);
        Task<ResultResponseDto<object>> InviteBulkUser(InviteBulkUserDto inviteUser, UserRole userRole, int userId);
        Task<ResultResponseDto<object>> DeleteUser(int deleteUserId, UserRole userRole, int userId);
        Task<ResultResponseDto<UserResponseDto>> RefreshToken(int userId);
        Task<ResultResponseDto<string>> SendMailForEditAssessment(SendRequestMailToUpdateCity request);
        Task<ResultResponseDto<UserResponseDto>> CityUserSignUp(CityUserSignUpDto request);
        Task<ResultResponseDto<object>> ConfirmMail(string passwordToken);
        Task<ResultResponseDto<object>> ContactUs(ContactUsRequestDto passwordToken);
        Task<ResultResponseDto<UserResponseDto>> TwofaVerification(string email, int otp);
        Task<ResultResponseDto<string>> ReSendLoginOtp(string email);
        Task<ResultResponseDto<string>> AddUpdateInvitation(UpdateInviteUserDto inviteUser, UserRole userRole, int userId);
    }
}
