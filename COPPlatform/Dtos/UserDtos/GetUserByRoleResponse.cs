using COPPlatform.Dtos.CityDto;

namespace COPPlatform.Dtos.UserDtos
{
    public class GetUserByRoleResponse : PublicUserResponse
    {
        public List<AddUpdateCityDto> cities { get; set; } = new();
    }
}
