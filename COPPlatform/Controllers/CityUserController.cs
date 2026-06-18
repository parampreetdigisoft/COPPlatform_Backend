using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.CityUserDto;
using COPPlatform.Enums;
using COPPlatform.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COPPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CityUserController : ControllerBase
    {
        private readonly ICityUserService _cityUserService;

        public CityUserController(ICityUserService cityUserService)
        {
            _cityUserService = cityUserService;
        }
        private int? GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;

            return null;
        }
        private string? GetTierFromClaims()
        {
            return User.FindFirst("Tier")?.Value;
        }

        [HttpPost("addCityUserKpisCityAndPillar")]
        public async Task<IActionResult> AddCityUserKpisCityAndPillar([FromBody] AddCityUserKpisCityAndPillar b)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized("User ID not found in token.");

            var tierName = GetTierFromClaims();
            if (tierName == null)
                return Unauthorized("You Don't have access.");

            if (!Enum.IsDefined(typeof(TieredAccessPlan), tierName))
                return Unauthorized("Invalid tier specified.");

            var response = await _cityUserService.AddCityUserKpisCityAndPillar(b, userId.GetValueOrDefault(), tierName);
            return Ok(response);
        }
        [HttpGet]
        [Route("getCityUserKpi")]
        public async Task<IActionResult> GetCityUserKpi()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized("User ID not found in token.");

            var tierName = GetTierFromClaims();
            if (tierName == null)
                return Unauthorized("You Don't have access.");

            var result = await _cityUserService.GetCityUserKpi(userId.GetValueOrDefault(), tierName);
            return Ok(result);
        }


    }
}
