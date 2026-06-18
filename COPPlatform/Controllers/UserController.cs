using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.EmailDto;
using COPPlatform.Dtos.UserDtos;
using COPPlatform.IServices;
using COPPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace COPPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest req)
        {

            return Created($"", new() { });
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
        [HttpGet]
        [Route("GetUserByRoleWithAssignedCity")]
        public async Task<IActionResult> GetUserByRoleWithAssignedCity([FromQuery] GetUserByRoleRequestDto request)
        {
            var claimUserId = GetUserIdFromClaims();
            if (claimUserId == null)
                return Unauthorized("User ID not found.");

            var role = GetRoleFromClaims();
            if (role == null)
                return Unauthorized("You Don't have access.");

            if (!Enum.TryParse<UserRole>(role, true, out var userRole))
            {
                return Unauthorized("You Don't have access.");
            }
            request.UserID = claimUserId.GetValueOrDefault();


            return Ok(await _userService.GetUserByRoleWithAssignedCity(request, userRole));
        }

        [HttpGet]
        [Route("getAccessUsers")]
        public async Task<IActionResult> GetAccessUsers([FromQuery] GetAssignUserDto request)
        {
            var claimUserId = GetUserIdFromClaims();
            if (claimUserId == null)
                return Unauthorized("User ID not found.");

            var role = GetRoleFromClaims();
            if (role == null)
                return Unauthorized("You Don't have access.");

            if (!Enum.TryParse<UserRole>(role, true, out var userRole))
            {
                return Unauthorized("You Don't have access.");
            }

            return Ok(await _userService.GetAccessUsers(request, userRole , claimUserId.GetValueOrDefault()));
        }

        [HttpPost]
        [Route("updateUser")]
        public async Task<IActionResult> UpdateUser([FromForm] UpdateUserDto dto)
        {
            var claimUserId = GetUserIdFromClaims();
            if (claimUserId == null || claimUserId != dto.UserID)
                return Unauthorized("User ID not found.");

            return Ok(await _userService.UpdateUser(dto));
        }

        [HttpGet]
        [Route("getUserInfo")]
        public async Task<IActionResult> getUserInfo()
        {
            var claimUserId = GetUserIdFromClaims();
            if (claimUserId == null )
                return Unauthorized("User ID not found.");

            return Ok(await _userService.GetUserInfo(claimUserId.GetValueOrDefault()));
        }

       
        [HttpGet]
        [Route("getInviations")]
        public async Task<IActionResult> GetInviations([FromQuery] GetInvitationRequestDto request)
        {
            var claimUserId = GetUserIdFromClaims();
            if (claimUserId == null)
                return Unauthorized("User ID not found.");

            var role = GetRoleFromClaims();
            if (role == null)
                return Unauthorized("You Don't have access.");

            if (!Enum.TryParse<UserRole>(role, true, out var userRole))
            {
                return Unauthorized("You Don't have access.");
            }

            return Ok(await _userService.GetInvitations(request, userRole, claimUserId.GetValueOrDefault()));
        }

        [HttpPost]
        [Route("deleteInvitation")]
        public async Task<IActionResult> DeleteInvitation([FromBody] DeleteInvitationDto request)
        {
            var claimUserId = GetUserIdFromClaims();
            if (claimUserId == null)
                return Unauthorized("User ID not found.");

            var role = GetRoleFromClaims();
            if (role == null)
                return Unauthorized("You Don't have access.");

            if (!Enum.TryParse<UserRole>(role, true, out var userRole))
            {
                return Unauthorized("You Don't have access.");
            }
            var response = await _userService.DeleteInvitation(request, userRole, claimUserId.GetValueOrDefault());

            if (response == null)
                return StatusCode(500, "User Invitation failed due to a server error.");

            return Ok(response);
        }

        [HttpPost]
        [Route("sendEmail")]
        public async Task<IActionResult> SendEmail([FromBody] SendEmailDto request)
        {
            var claimUserId = GetUserIdFromClaims();
            if (claimUserId == null)
                return Unauthorized("User ID not found.");

            var role = GetRoleFromClaims();
            if (role == null)
                return Unauthorized("You Don't have access.");

            if (!Enum.TryParse<UserRole>(role, true, out var userRole))
            {
                return Unauthorized("You Don't have access.");
            }
            var response = await _userService.SendEmail(request, userRole, claimUserId.GetValueOrDefault());

            if (response == null)
                return StatusCode(500, "User Invitation failed due to a server error.");

            return Ok(response);
        }

        [HttpGet]
        [Route("getSendEmailLogs")]
        [Authorize]
        public async Task<IActionResult> GetSendEmailLogs([FromQuery] EmailLogRequestDto response)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized("User ID not found in token.");

            var role = GetRoleFromClaims();
            if (role == null)
                return Unauthorized("You Don't have access.");

            if (!Enum.TryParse<UserRole>(role, true, out var userRole))
            {
                return Unauthorized("You Don't have access.");
            }

            response.UserId = userId.GetValueOrDefault();

            var result = await _userService.GetEmailLogs(response, userRole, userId.Value);
            return Ok(result);
        }
    }

    public class RegisterRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
        public UserRole Role { get; set; }
    }
} 