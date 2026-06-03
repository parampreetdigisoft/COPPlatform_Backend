using COPPlatform.Models;

namespace COPPlatform.Dtos.dashboard
{
    /// <summary>
    /// Query/projection models used when building executive card details (not API responses).
    /// </summary>
    public class AssessmentScoreRow
    {
        public int UserAssessmentMappingID { get; set; }
        public IEnumerable<PillarAssessmentScoreRow> PillarAssessments { get; set; } =
            Array.Empty<PillarAssessmentScoreRow>();
    }

    public class PillarAssessmentScoreRow
    {
        public int PillarID { get; set; }
        public IEnumerable<ScoreValue?> Responses { get; set; } = Array.Empty<ScoreValue?>();
    }

    public class PillarQuestionCountRow
    {
        public int PillarID { get; set; }
        public string PillarName { get; set; } = "";
        public int QuestionCount { get; set; }
    }

    public class RiskMappingRow
    {
        public int UserAssessmentMappingID { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string AssessmentName { get; set; } = "";
        public string OwnerName { get; set; } = "";
    }

    public class ExecutiveRiskMetrics
    {
        public int Overdue { get; set; }
        public int HighRisk { get; set; }
        public int AtRisk { get; set; }
        public int DueSoon { get; set; }
        public int OnTrack { get; set; }
        public List<RiskDetailDto> Details { get; set; } = new();
    }
}
