
using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.QuestionDto;
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
    public class QuestionController : ControllerBase
    {
        private readonly IQuestionService _questionService;
        public QuestionController(IQuestionService questionService)
        {
            _questionService = questionService;
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

        [HttpGet("pillars")]
        [Authorize]
        public async Task<IActionResult> GetPillars() => Ok(await _questionService.GetPillarsAsync());

        [HttpGet("getQuestions")]
        [Authorize]
        public async Task<IActionResult> GetQuestions([FromQuery] GetQuestionRequestDto requestDto) => Ok(await _questionService.GetQuestionsAsync(requestDto));

        [HttpPost("add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddQuestion([FromBody] Question q)
        {
            var result = await _questionService.AddQuestionAsync(q);
            return Ok(result);
        }
        [HttpPost("addUpdateQuestion")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddUpdateQuestion([FromBody] AddUpdateQuestionDto q)
        {
            var result = await _questionService.AddUpdateQuestion(q);
            return Ok(result);
        }

        [HttpPost("addBulkQuestions")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddBulkQuestions([FromBody] AddBulkQuestionsDto q)
        {
            var result = await _questionService.AddBulkQuestion(q);
            return Ok(result);
        }

        [HttpPut("edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditQuestion(int id, [FromBody] Question q)
        {
            var result = await _questionService.EditQuestionAsync(id, q);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var success = await _questionService.DeleteQuestionAsync(id);
            if (!success) return BadRequest("You don't have Access");
            return Ok();
        }

        [HttpGet("getQuestionsByAssessmentMappingId")]
        [Authorize]
        public async Task<IActionResult> GetQuestionsByAssessmentMappingId([FromQuery] CityPillerRequestDto requestDto)
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

            var result = await _questionService.GetQuestionsByAssessmentMappingId(requestDto, userId.GetValueOrDefault(), userRole);
            if (result == null) return NotFound();

            return Ok(result);
        }
        
        [HttpGet("ExportAssessment/{UserAssessmentMappingID}")]
        [Authorize]
        public async Task<IActionResult> ExportAssessment(int UserAssessmentMappingID)
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

            var content = await _questionService.ExportAssessment(UserAssessmentMappingID, userId.GetValueOrDefault() , userRole);

            return File(content.Item2 ?? new byte[1],
               "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
               content.Item1);
        }

        [HttpGet("getQuestionsHistoryByPillar")]
        [Authorize]
        public async Task<IActionResult> GetQuestionsHistoryByPillar([FromQuery] GetCityPillarHistoryRequestDto requestDto)
        {
            var content = await _questionService.GetQuestionsHistoryByPillar(requestDto);

            return Ok(content);
        }
    }
}
