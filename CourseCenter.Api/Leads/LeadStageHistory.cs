namespace CourseCenter.Api.Leads
{
    public class LeadStageHistory
    {
        public int Id { get; set; }
        public int LeadId { get; set; }
        public Lead Lead { get; set; } = null!;
        public int? FromStageId { get; set; }
        public LeadStage? FromStage { get; set; }
        public int ToStageId { get; set; }
        public LeadStage ToStage { get; set; } = null!;
        public int ChangedByUserId { get; set; }
        public User ChangedByUser { get; set; } = null!;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
