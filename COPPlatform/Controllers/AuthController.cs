using COPPlatform.Dtos.UserDtos;
using COPPlatform.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace COPPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        private int? GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;

            return null;
        }
        private string? GetRoleFromClaims()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _authService.Login(request.Email, request.Password);
            if (user == null)
                return Unauthorized();
            return Ok(user);
        }
        
        [HttpPost]
        [Route("forgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            if (request?.Email == null)
                return BadRequest("Invalid request data.");

            var response = await _authService.ForgotPassword(request.Email);

            if (response == null)
                return StatusCode(500, "Password reset failed due to a server error.");

            return Ok(response);
        }

        [HttpPost]
        [Route("changePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangedPasswordDto request)
        {
            if (request?.PasswordToken == null || request.Password == null)
                return BadRequest("Invalid request data.");

            var response = await _authService.ChangePassword(request.PasswordToken, request.Password);

            if (response == null)
                return StatusCode(500, "User registration failed due to a server error.");

            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        [Route("addUpdateStaffUser")]
        public async Task<IActionResult> AddStaffUser([FromBody] RegisterDto request)
        {
            if (request?.Email == null)
                return BadRequest("Invalid request data.");

            var claimUserId = GetUserIdFromClaims();
            if (claimUserId == null)
                return Unauthorized("User ID not found.");

            var role = GetRoleFromClaims();
            if (role == null)
                return Unauthorized("You Don't have access.");

            if (!Enum.TryParse<Models.UserRole>(role, true, out var userRole))
            {
                return Unauthorized("You Don't have access.");
            }

            var response = await _authService.AddStaffUser(request,userRole, claimUserId.GetValueOrDefault());

            if (response == null)
                return StatusCode(500, "User Invitation failed due to a server error.");

            return Ok(response);
        }

        [HttpPost]
        [Route("InviteBulkUser")]
        [Authorize]
        public async Task<IActionResult> InviteBulkUser([FromBody] InviteBulkUserDto request)
        {
            var claimUserId = GetUserIdFromClaims();
            if (claimUserId == null)
                return Unauthorized("User ID not found.");

            var role = GetRoleFromClaims();
            if (role == null)
                return Unauthorized("You Don't have access.");

            if (!Enum.TryParse<Models.UserRole>(role, true, out var userRole))
            {
                return Unauthorized("You Don't have access.");
            }

            var response = await _authService.InviteBulkUser(request, userRole, claimUserId.GetValueOrDefault());

            if (response == null)
                return StatusCode(500, "User Invitation failed due to a server error.");

            return Ok(response);
        }

        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public IActionResult Register([FromBody] RegisterRequest req)
        {
            if (_authService.GetByEmail(req.Email) != null)
                return BadRequest("User already exists");
            var user = _authService.Register(req.FullName, req.Email, req.Phone, req.Password, req.Role);
            return Created($"/api/user/{user.UserID}", new { user.UserID, user.FullName, user.Email, user.Role });
        }

        [HttpDelete("deleteUser/{deleteUserId}")]
        public async Task<IActionResult> DeleteUser(int deleteUserId)
        {
            var claimUserId = GetUserIdFromClaims();
            if (claimUserId == null)
                return Unauthorized("User ID not found.");

            var role = GetRoleFromClaims();
            if (role == null)
                return Unauthorized("You Don't have access.");

            if (!Enum.TryParse<Models.UserRole>(role, true, out var userRole) || userRole == Models.UserRole.Evaluator)
            {
                return Unauthorized("You Don't have access.");
            }

            var result = await _authService.DeleteUser(deleteUserId, userRole, claimUserId.GetValueOrDefault());
            return Ok(result);
        }

        [HttpPost("refreshToken")]
        [Authorize]
        public async Task<IActionResult> RefreshToken([FromBody] UserIdDto request)
        {
            var user = await _authService.RefreshToken(request.UserID);
            if (user == null)
                return Unauthorized();
            return Ok(user);
        }


        [HttpPost]
        [Route("confirmMail")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmMail([FromBody] ConfirmMailDto request)
        {
            if (request?.PasswordToken == null)
                return BadRequest("Invalid request data.");

            var response = await _authService.ConfirmMail(request.PasswordToken);

            if (response == null)
                return StatusCode(500, "Mail not confirmed due to a server error.");

            return Ok(response);
        }
        [HttpPost]
        [Route("contactus")]
        [AllowAnonymous]
        public async Task<IActionResult> ContactUs([FromBody] ContactUsRequestDto request)
        {
            var response = await _authService.ContactUs(request);

            if (response == null)
                return StatusCode(500, "Mail not confirmed due to a server error.");

            return Ok(response);
        }

        [HttpPost("twofaVerification")]
        public async Task<IActionResult> TwofaVerification([FromBody] TwofaVerificationRequest request)
        {
            var user = await _authService.TwofaVerification(request.Email, request.Otp);
            if (user == null)
                return Unauthorized();
            return Ok(user);
        }
        [HttpPost("reSendLoginOtp")]
        public async Task<IActionResult> ReSendLoginOtp([FromBody] EmailRequest request)
        {
            var user = await _authService.ReSendLoginOtp(request.Email);
            if (user == null)
                return Unauthorized();
            return Ok(user);
        }

        [HttpPost]
        [Route("addUpdateInvitation")]
        public async Task<IActionResult> AddUpdateInvitation([FromBody] UpdateInviteUserDto request)
        {
            var claimUserId = GetUserIdFromClaims();
            if (claimUserId == null)
                return Unauthorized("User ID not found.");

            var role = GetRoleFromClaims();
            if (role == null)
                return Unauthorized("You Don't have access.");

            if (!Enum.TryParse<Models.UserRole>(role, true, out var userRole) || userRole == Models.UserRole.Evaluator)
            {
                return Unauthorized("You Don't have access.");
            }
            var response = await _authService.AddUpdateInvitation(request, userRole, claimUserId.GetValueOrDefault());

            if (response == null)
                return StatusCode(500, "User Invitation failed due to a server error.");

            return Ok(response);
        }
    }

    public class LoginRequest : EmailRequest
    {
        public string Password { get; set; }
    }
    public class TwofaVerificationRequest : EmailRequest
    {
        public int Otp { get; set; }
    }
    public class EmailRequest
    {
        public string Email { get; set; }
    }
} 