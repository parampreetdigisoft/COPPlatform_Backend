namespace COPPlatform.Models
{
    public class PillarAssessmentProgressResult
    {
        public int PillarID { get; set; }
        public string? PillarName { get; set; }
        public int? DisplayOrder { get; set; }
        public int UserAssessmentMappingID { get; set; }
        public string? GeographicReference { get; set; }
        public int TotalScore { get; set; }
        public int TotalAns { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalCriticalQuestions { get; set; }
        public int TotalAnsweredCriticalQuestions { get; set; }
        public decimal? ScoreProgress { get; set; }
        public decimal? CompletionRate { get; set; }
    }
}
