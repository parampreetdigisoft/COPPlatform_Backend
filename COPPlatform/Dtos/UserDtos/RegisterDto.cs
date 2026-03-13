using COPPlatform.Models;

namespace COPPlatform.Dtos.UserDtos
{
    public class RegisterDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; } = "sdfjru32brjfew";
        public UserRole Role { get; set; }
    }

    public class UpdateInviteUserDto 
    {
        public int? UserAssessmentMappingID { get; set; }
        public int UserID { get; set; }
        public string GeographicReference  { get; set; }
        public DateTime? DueDate { get; set; }
        public int Year { get; set; }
        public List<int> PillarIDs { get; set; }
    }
    public class InviteBulkUserDto
    {
        public List<RegisterDto> users { get; set; }
    }
}
