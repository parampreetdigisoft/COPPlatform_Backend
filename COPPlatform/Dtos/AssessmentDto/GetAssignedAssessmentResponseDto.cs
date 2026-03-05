using COPPlatform.Models;

namespace COPPlatform.Dtos.AssessmentDto
{
    public class GetAssignedAssessmentResponseDto
    {
        public int UserAssessmentMappingID { get; set; }
        public int Year { get; set; }
        public int UserID { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<AssignedAssessmentPillarMappingDto>? UserPillarMappings { get; set; }
        public string AssignedBy { get; set; }
    }

    public class AssignedAssessmentPillarMappingDto
    {
        public int UserPillarMappingID { get; set; }
        public int Year { get; set; }
        public int UserID { get; set; }
        public DateTime? DueDate { get; set; }
        public int PillarID { get; set; }
        public string PillarName { get; set; }
        public string Description { get; set; }
        public int DisplayOrder { get; set; }
        public string ImagePath { get; set; }
    }
}
