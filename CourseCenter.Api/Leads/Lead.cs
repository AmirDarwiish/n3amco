using CourseCenter.Api.Users;

namespace CourseCenter.Api.Leads
{
    public class Lead
    {
        public int Id { get; set; }

        // Contact Info
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }

        // Sales
        public LeadStatus Status { get; set; } = LeadStatus.New;
        public string? LostReason { get; set; }


        public string Source { get; set; } = null!; // Facebook, Call, WhatsApp

        // Assignment
        public int? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<LeadNote> Notes { get; set; } = new List<LeadNote>();
        public DateTime? FollowUpDate { get; set; }
        public string? FollowUpReason { get; set; }

        public bool IsArchived { get; set; } = false; 
        public DateTime? ArchivedAt { get; set; }     
        public int? ArchivedByUserId { get; set; }    


    }
}
