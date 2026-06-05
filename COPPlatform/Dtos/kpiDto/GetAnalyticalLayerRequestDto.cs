using COPPlatform.Dtos.CommonDto;
using COPPlatform.Models;

namespace COPPlatform.Dtos.kpiDto
{
    public class GetAnalyticalLayerRequestDto : PaginationRequest
    {
        public int UserAssessmentMappingID { get; set; }
        public int? LayerID { get; set; }
    }
}
