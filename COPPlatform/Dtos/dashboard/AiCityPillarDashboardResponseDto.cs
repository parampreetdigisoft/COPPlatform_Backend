namespace COPPlatform.Dtos.dashboard
{
    public class AiCityPillarDashboardResponseDto
    {
        public int? UserAssessmentMappingID { get; set; }
        public string GeographicReference { get; set; }
        public decimal ScoreProgress { get; set; }
        public List<CityPillarDashboardPillarValueDto> Pillars { get; set; } = new List<CityPillarDashboardPillarValueDto>();
    }

    public class CityPillarDashboardPillarValueDto
    {
        public int PillarID { get; set; }
        public string PillarName { get; set; }
        public int DisplayOrder { get; set; }
        public int TotalScore { get; set; }
        public int TotalAns { get; set; }
        public int TotalQuestions { get; set; }

        public int TotalCriticalQuestions { get; set; }

        public int TotalAnsweredCriticalQuestions { get; set; }
        public decimal ScoreProgress { get; set; }
        public decimal CompletionRate { get; set; }

    }
}
