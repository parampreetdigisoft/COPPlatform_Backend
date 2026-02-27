namespace COPPlatform.Models
{
    public class PillarAssessment
    {
        public int PillarAssessmentID { get; set; }
        public int AssessmentID { get; set; }
        public int PillarID { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public Assessment? Assessment { get; set; }
        public Pillar? Pillar { get; set; }
        public ICollection<AssessmentResponse> Responses { get; set; } = new List<AssessmentResponse>();
    }
}
