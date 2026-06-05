using COPPlatform.Dtos.CommonDto;

namespace COPPlatform.Dtos.kpiDto
{
    public class GetKpiLayerChartRequestDto : PaginationRequest
    {
        public int UserAssessmentMappingID { get; set; }
        public List<int> LayerIDs { get; set; } = new();
    }
}
