using COPPlatform.Common.Models;
using COPPlatform.Dtos.AssessmentDto;
using COPPlatform.Dtos.CommonDto;
using COPPlatform.Dtos.PillarDto;
using COPPlatform.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace COPPlatform.IServices
{
    public interface IPillarService
    {
        Task<List<Pillar>> GetAllAsync();
        Task<Pillar> GetByIdAsync(int id);
        Task<Pillar> AddAsync(Pillar pillar);
        Task<Pillar> UpdateAsync(int id, UpdatePillarDto pillar);
        Task<bool> DeleteAsync(int id);
        Task<Tuple<string, byte[]>> ExportPillarsHistoryByUserId(GetCityPillarHistoryRequestDto requestDto);
        Task<ResultResponseDto<List<PillarsHistroyResponseDto>>> GetResponsesByUserId(GetPillarResponseHistoryRequestNewDto request, int userId, UserRole userRole);

    }
} 