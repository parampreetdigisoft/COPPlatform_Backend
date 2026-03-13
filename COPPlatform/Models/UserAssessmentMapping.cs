namespace COPPlatform.Models
{
    public class UserAssessmentMapping
    {
        public int UserAssessmentMappingID { get; set; }
        public string GeographicReference { get; set; }
        public int Year { get; set; }
        public int UserID { get; set; }
        public UserRole Role { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<UserPillarMapping>? UserPillarMappings { get; set; }
        public User? User { get; set; }
        public int AssignedByUserId { get; set; } // need to replace it

        public int CityID { get; set; } // need to replace it
    }
    public class UserPillarMapping
    {
        public int UserPillarMappingID { get; set; }
        public int UserAssessmentMappingID { get; set; }
        public int Year { get; set; }
        public int UserID { get; set; }
        public int PillarID { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public int AssignedByUserId { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public User? User { get; set; }
        public Pillar? Pillar { get; set; }
        public UserAssessmentMapping? UserAssessmentMapping { get; set; }
    }
}
