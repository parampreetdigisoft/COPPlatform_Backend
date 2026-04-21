using COPPlatform.Dtos.CommonDto;

namespace COPPlatform.Dtos.AssessmentDto
{
    public class GetAssessmentQuestionRequestDto : PaginationRequest
    {
        public int AssessmentID { get; set; } 
        public int? PillarID { get; set; }
    }
}
