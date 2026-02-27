using COPPlatform.Models;

namespace COPPlatform.Dtos.UserDtos
{
    public class GetAssignUserDto
    {
        public int? SearchedUserID { get; set; }
        public UserRole UserRole { get; set; }
    }
    public class UserIdDto
    {
        public int UserID { get; set; }
    }
}
