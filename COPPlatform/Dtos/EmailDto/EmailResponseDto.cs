namespace COPPlatform.Dtos.EmailDto
{
    public class EmailLogResponseDto
    {
        public int Id { get; set; }

        public int? SenderUserId { get; set; }
        public string SenderEmail { get; set; }

        public string ReceiverEmail { get; set; }

        public string Subject { get; set; }
        public string Message { get; set; }

        public string TemplateName { get; set; }

        public bool IsSent { get; set; }
        public string ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
