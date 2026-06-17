namespace COPPlatform.Dtos.kpiDto
{
    public class GetExecutiveKpiDashboardResponseDto
    {
        public ExecutiveKpiDashboardSummaryDto Summary { get; set; } = new();
        public List<ExecutiveKpiLayerGroupDto> OverallKpis { get; set; } = new();
        public List<ExecutivePillarKpiGroupDto> PillarGroups { get; set; } = new();
    }

    public class ExecutiveKpiDashboardSummaryDto
    {
        public int OverallKpiCount { get; set; }
        public int PillarCount { get; set; }
        public int TotalKpiRecords { get; set; }
        public decimal OverallReadinessScore { get; set; }
        public int CriticalCount { get; set; }
        public int AtRiskCount { get; set; }
        public int OnTrackCount { get; set; }
    }

    public class ExecutiveKpiLayerGroupDto
    {
        public int LayerID { get; set; }
        public string LayerCode { get; set; } = string.Empty;
        public string LayerName { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string? CalText { get; set; }
        public int? InterpretationID { get; set; }
        public decimal? CalValue { get; set; }
        public string? Condition { get; set; }
        public int ConditionLevel { get; set; }
        public ICollection<Models.FiveLevelInterpretation> FiveLevelInterpretations { get; set; } = new List<Models.FiveLevelInterpretation>();
        public GetAnalyticalLayerResultDto? Detail { get; set; }
    }

    public class ExecutivePillarKpiGroupDto
    {
        public int PillarID { get; set; }
        public string PillarName { get; set; } = string.Empty;
        public decimal AvgScore { get; set; }
        public int KpiCount { get; set; }
        public List<ExecutiveKpiLayerGroupDto> Kpis { get; set; } = new();
    }
}
