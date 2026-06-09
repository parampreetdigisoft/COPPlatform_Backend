using COPPlatform.Dtos.CommonDto;
using COPPlatform.Models;

namespace COPPlatform.Dtos.AssessmentDto
{
    public class GetAssessmentRequestDto : PaginationRequest
    {
        public int? UserAssessmentMappingID { get; set; }
        public UserRole? Role { get; set; }
    }
}
    