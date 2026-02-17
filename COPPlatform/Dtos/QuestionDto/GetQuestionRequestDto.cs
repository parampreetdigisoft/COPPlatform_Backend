using COPPlatform.Dtos.CommonDto;

namespace COPPlatform.Dtos.QuestionDto
{
    public class GetQuestionRequestDto : PaginationRequest
    {
        public int? PillarID { get; set; }
    }
}
