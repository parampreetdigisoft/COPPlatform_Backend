using COPPlatform.Common.Models;
using COPPlatform.Dtos.CommonDto;
using COPPlatform.Dtos.PublicDto;

namespace COPPlatform.IServices
{
    public interface IPublicService
    {
        Task<ResultResponseDto<List<PillarResponseDto>>> GetAllPillarAsync();

    }
}
