using COPPlatform.Dtos.CommonDto;

namespace COPPlatform.Dtos.EmailDto
{
    public class EmailLogRequestDto : PaginationRequest
    {
        public int? SenderUserId { get; set; }
        public string? ReceiverEmail { get; set; }
        public bool? IsSent { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
