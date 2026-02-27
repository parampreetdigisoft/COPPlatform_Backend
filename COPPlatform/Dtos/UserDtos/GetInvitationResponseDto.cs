
using COPPlatform.Dtos.PublicDto;
using COPPlatform.Models;

namespace COPPlatform.Dtos.UserDtos
{
    public class GetInvitationResponseDto : InviationUserResponseDto
    {
        public List<InvitationPillarResponseDto> Pillars { get; set; } = new();
    }

    public class InvitationPillarResponseDto : PillarResponseDto
    {
        public int UserPillarMappingID { get; set; }

    }
    public class InviationUserResponseDto
    {
        public int UserAssessmentMappingID { get; set; }
        public int Year { get; set; }
        public DateTime? DueDate { get; set; }
        public int NumOfUser { get; set; }
        public int UserID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
