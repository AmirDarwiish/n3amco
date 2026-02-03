using System;

namespace CourseCenter.Api.CustomerHistory
{
    public class CustomerHistoryDto
    {
        public int Id { get; set; }
        public Guid CustomerId { get; set; }
        public string EventType { get; set; } = null!;
        public string SourceEntity { get; set; } = null!;
        public string? SourceEntityId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
    }
}
