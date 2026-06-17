using COPPlatform.Dtos.CommonDto;

namespace COPPlatform.Dtos.kpiDto
{
    public class GetExecutiveOverviewKpisRequestDto : PaginationRequest
    {
        /// <summary>
        /// The assessment selection (userAssessmentMappingID) that KPI values are calculated from.
        /// </summary>
        public int UserAssessmentMappingID { get; set; }

        public int? LayerID { get; set; }

        /// <summary>
        /// For now we only show non-pillar KPIs (pillarId null/0). Keep this switch for the future.
        /// </summary>
        public bool IncludePillarKpis { get; set; } = false;
    }
}

