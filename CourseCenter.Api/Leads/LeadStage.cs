namespace CourseCenter.Api.Leads
{
    public class LeadStage
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public bool IsFinal { get; set; }
        public bool IsWon { get; set; }
        public bool IsLost { get; set; }
    }
}
