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
        public string GeographicReference { get; set; }    

    }

    public class AssignedAssessmentPillarMappingDto :PillarInformationDTO
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

    public class PillarInformationDTO
    {
        public int TotalScore { get; set; }
        public int TotalAnsweredQuestions { get; set; }
        public int TotalQuestions { get; set; }
        public decimal ScoreProgress { get; set; }
        public decimal CompletionRate { get; set; }

        public int TotalCriticalQuestions { get; set; }

        public int TotalCriticalAnsweredQuestions { get; set; }

    }
}
