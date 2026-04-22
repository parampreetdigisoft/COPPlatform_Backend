namespace COPPlatform.Dtos.PillarDto
{
    public class PillarsHistroyResponseDto
    {
        public int PillarID { get; set; }
        public string PillarName { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public int UserAssessmentMappingID { get; set; } = 0;
        public List<PillarsUserHistroyResponseDto> Users { get; set; } = new();

        public string WeekType { get; set; }

    }
    public class PillarsUserHistroyResponseDto
    {
        public int UserID { get; set; }
        public string FullName { get; set; }
        public decimal ScoreProgress { get; set; }
        public decimal CompeletionRate { get; set; }
        public int TotalQuestion { get; set; }
        public int AnsQuestion { get; set; }
    }

    public class WeeklyPillarsHistoryResponseDto
    {
        public List<PillarsHistroyResponseDto> Week1 { get; set; }
        public List<PillarsHistroyResponseDto> Week2 { get; set; }
    }

}
