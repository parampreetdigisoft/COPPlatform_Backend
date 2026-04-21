namespace COPPlatform.Dtos.dashboard
{
    public class CardDetailsDto
    {
        public int TotalExecutives { get; set; }
        public int TotalAnalysts { get; set; }
        public int TotalEvaluators { get; set; }

        public int TotalOverdue { get; set; }
        public int TotalHighRisk { get; set; }
        public int TotalAtRisk { get; set; }
        public int TotalDueSoon { get; set; }
        public int TotalOnTrack { get; set; }

        public int TotalAssessments { get; set; }
        public int TotalCompletedAssessments { get; set; }
        public int TotalInProgressAssessments { get; set; }

        public decimal AveragePillarScore { get; set; }
        public PillarCardDetailsDto HighestPillarScore { get; set; } = new PillarCardDetailsDto();
        public PillarCardDetailsDto LowestPillarScore { get; set; } = new PillarCardDetailsDto();
    }

    public class PillarCardDetailsDto
    {
        public int PillarID { get; set; }
        public string PillarName { get; set; }
        public decimal Value { get; set; }
    }
}
