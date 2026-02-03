using System;

namespace CourseCenter.Api.CustomerHistory
{
    public class CustomerHistory
    {
        public int Id { get; set; }
        // CustomerId is stored as a Guid reference to avoid assuming a Customer entity exists.
        public Guid CustomerId { get; set; }
        public string EventType { get; set; } = null!;
        public string SourceEntity { get; set; } = null!;
        public string? SourceEntityId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // CreatedBy can be linked to Users via FK configuration when available.
        public int CreatedBy { get; set; }
    }
}
