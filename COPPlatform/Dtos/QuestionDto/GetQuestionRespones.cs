using COPPlatform.Models;

namespace COPPlatform.Dtos.QuestionDto
{
    public class GetQuestionRespones : AddUpdateQuestionDto
    {
        public int DisplayOrder { get; set; }
        public string PillarName { get; set; }
    }
    public class GetQuestionByCityRespones : GetQuestionRespones
    {
        public int AssessmentID { get; set; }
        public int PillarDisplayOrder { get; set; }
    }
    public class GetPillarQuestionByCityRespones  : AssessmentPillarsDto
    {
        public int AssessmentID { get; set; }
        public int UserAssessmentMappingID { get; set; }
        public int SubmittedPillarDisplayOrder { get; set; }
        public List<AssessmentPillarsDto> Pillars { get; set; }
        public List<AssessmentQuestionResponseDto> Questions { get; set; }
    }

    public class AssessmentPillarsDto
    {
        public int PillarID { get; set; }
        public string PillarName { get; set; }
        public int DisplayOrder { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; }
    }
}
