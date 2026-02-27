using COPPlatform.Common.Models;
using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.CommonDto;
using COPPlatform.Dtos.UserDtos;
using COPPlatform.Models;

namespace COPPlatform.IServices
{
    public interface IUserService
    {
        User GetByEmail(string email);
        Task<PaginationResponse<GetUserByRoleResponse>> GetUserByRoleWithAssignedCity(GetUserByRoleRequestDto requestDto,UserRole userRole);
        Task<ResultResponseDto<List<PublicUserResponse>>> GetAccessUsers(GetAssignUserDto requestDto, UserRole userRole, int userId);
        Task<ResultResponseDto<UpdateUserResponseDto>> UpdateUser(UpdateUserDto requestDto);
        Task<ResultResponseDto<List<GetAssessmentResponseDto>>> GetUsersAssignedToCity(int cityId);
        Task<ResultResponseDto<UpdateUserResponseDto>> GetUserInfo(int userId);
        Task<PaginationResponse<GetInvitationResponseDto>> GetInvitations(GetInvitationRequestDto request, UserRole userRole, int userId);
        Task<ResultResponseDto<string>> DeleteInvitation(DeleteInvitationDto request, UserRole userRole, int userId);

    }
} 