using COPPlatform.Models;

namespace COPPlatform.Dtos.AssessmentDto
{
    public class GetAssessmentResponseDto
    {
        public int AssessmentID { get; set; }
        public int UserAssessmentMappingID { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string GeographicReference { get; set; }
        public int Year { get; set; }
        public int UserID { get; set; }
        public UserRole Role { get; set; }
        public DateTime? DueDate { get; set; }
        public string AnalystName { get; set; }
        public decimal Score { get; set; }
        public AssessmentPhase? AssessmentPhase { get; set; }
    }
}
