using System;

namespace COPPlatform.Models
{
    public enum ScoreValue { Four = 4, Three = 3, Two = 2, One = 1, Zero = 0, NA, Unknown }
    public class AssessmentResponse
    {
        public int ResponseID { get; set; }
        public int PillarAssessmentID { get; set; }
        public int QuestionID { get; set; }
        public int QuestionOptionID { get; set; }
        public ScoreValue? Score { get; set; }
        public string Justification { get; set; } 
        public string? Source { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public PillarAssessment? PillarAssessment { get; set; } 
        public Question? Question { get; set; } 
        public ICollection<AssessmentResponseHistory> AssessmentResponseHistories { get; set; } = new List<AssessmentResponseHistory>();
    }

    public class AssessmentResponseHistory
    {
        public int ResponseHistoryID { get; set; }
        public int ResponseID { get; set; }
        public int UserID { get; set; }
        public int QuestionID { get; set; }
        public int QuestionOptionID { get; set; }
        public ScoreValue? Score { get; set; }
        public string Justification { get; set; }
        public string? Source { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public Question? Question { get; set; }
        public User? User { get; set; }
        public AssessmentResponse? AssessmentResponse { get; set; } 
    }

} 