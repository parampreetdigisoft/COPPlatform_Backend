using COPPlatform.Dtos.CommonDto;
using COPPlatform.Models;

namespace COPPlatform.Dtos.UserDtos
{
    public class GetInvitationRequestDto : PaginationRequest
    {
        public UserRole GetUserRole { get; set; }
        public int? Year { get; set; }
    }
}
