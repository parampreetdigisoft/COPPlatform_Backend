using COPPlatform.Models;

namespace COPPlatform.Dtos.AssessmentDto
{
    public class GetExecutiveAssignedAssessmentResponseDto
    {
        public int UserAssessmentMappingID { get; set; }
        public int Year { get; set; }
        public int UserID { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;        
        public string AssignedBy { get; set; }
        public string GeographicReference { get; set; }
        public int AvgTotalScore { get; set; }
        public int TotalAnsweredQuestions { get; set; }
        public int TotalQuestions { get; set; }
        public decimal AvgScoreProgress { get; set; }
        public decimal AvgCompletionRate { get; set; }
        public string BestPerformingPillar { get; set; }
        public string WorstPerformingPillar { get; set; }
        public decimal BestCompletionRate { get; set; }
        public decimal WorstCompletionRate { get; set; }
        public string RiskLevel { get; set; }
        public int DaysRemaining { get; set; }
        public decimal Progress { get; set; }

        public decimal OnTrackPercent { get; set; }
        public decimal OffTrackPercent { get; set; }
        public decimal AtRiskPercent { get; set; }

        public int TotalCriticalQuestions { get; set; }

        public int TotalCriticalAnsweredQuestions { get; set; }       

    }



}
