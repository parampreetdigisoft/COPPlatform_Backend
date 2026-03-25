using COPPlatform.Dtos.CommonDto;
using COPPlatform.Models;

namespace COPPlatform.Dtos.AssessmentDto
{
    public class GetAssessmentRequestDto : PaginationRequest
    {
        public int? SubUserID { get; set; } //Means admin or analyst can see result of a user that they has permission
        public int? CityID { get; set; }
        public UserRole? Role { get; set; }
        public DateTime UpdatedAt { get; set; } = new DateTime(DateTime.UtcNow.Year, 1, 1);
    }
}
    