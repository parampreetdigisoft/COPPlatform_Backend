namespace COPPlatform.Dtos.CityDto
{
    public class EvaluationCityProgressResultDto
    {
        public int PillarID { get; set; }
        public double Weight { get; set; }
        public bool Reliability { get; set; }
        public int UserAssessmentMappingID { get; set; }
        public int TotalScore { get; set; }
        public int TotalAns { get; set; }
        public int TotalCriticalQuestions { get; set; }
        public int TotalAnsweredCriticalQuestions { get; set; }
        public int TotalQuestions { get; set; }
        public decimal ScoreProgress { get; set; }
        public decimal CompletionRate { get; set; }
        public int UserID { get; set; }
    }
    public class UserEvaluationPillarProgressResultDto
    {
        public int PillarID { get; set; }
        public string PillarName { get; set; }
        public int DisplayOrder { get; set; }
        public int UserAssessmentMappingID { get; set; }
        public int TotalScore { get; set; }
        public int TotalAns { get; set; }
        public int TotalQuestions { get; set; }
        public decimal ScoreProgress { get; set; }
        public decimal CompletionRate { get; set; }
        public int SubmittedByUserID { get; set; }
        public string SubmittedByUserName { get; set; }

        public string? WeekType { get; set; }
    }

    public class WeeklyPillarRawDto
    {
        public int PillarID { get; set; }
        public string PillarName { get; set; }
        public int DisplayOrder { get; set; }
        public int UserAssessmentMappingID { get; set; }
        public int SubmittedByUserID { get; set; }
        public string SubmittedByUserName { get; set; }
        public int TotalScore { get; set; }
        public int TotalAns { get; set; }
        public int TotalQuestions { get; set; }
        public decimal ScoreProgress { get; set; }
        public decimal CompletionRate { get; set; }
    }


}
