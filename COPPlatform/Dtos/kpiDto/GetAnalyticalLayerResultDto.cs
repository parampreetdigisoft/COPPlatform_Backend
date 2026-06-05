using COPPlatform.Models;

namespace COPPlatform.Dtos.kpiDto
{
    public class GetAnalyticalLayerResultDto
    {
        public int UserAssessmentMappingID { get; set; }
        public string? GeographicReference { get; set; }
        public int? InterpretationID { get; set; }
        public decimal? CalValue { get; set; }
        public int? PillarID { get; set; }
        public string? PillarName { get; set; }

        public int LayerID { get; set; }
        public string LayerCode { get; set; } = string.Empty;
        public string LayerName { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string? CalText { get; set; }
        public ICollection<FiveLevelInterpretation> FiveLevelInterpretations { get; set; } = new List<FiveLevelInterpretation>();
    }
}
