using System;
using System.Collections.Generic;

namespace CourseCenter.Api.Leads.DTOs
{
    public class LeadDetailsDto
    {
        public LeadInfoDto LeadInfo { get; set; } = new();
        public StageDto CurrentStage { get; set; } = new();
        public List<StageDto> AllStages { get; set; } = new();
        public List<StageHistoryDto> StageHistory { get; set; } = new();
        public List<ActivityDto> ActivityTimeline { get; set; } = new();
        public MetricsDto Metrics { get; set; } = new();
    }

    public class LeadInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string? Source { get; set; }
        public string? LostReason { get; set; }
        public decimal? Budget { get; set; }
        public string? Location { get; set; }
        public string? InterestedIn { get; set; }
        public AssignedUserDto? AssignedUser { get; set; }
    }

    public class StageDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Order { get; set; }
    }

    public class StageHistoryDto
    {
        public string? FromStage { get; set; }
        public string ToStage { get; set; } = null!;
        public int ChangedBy { get; set; }
        public string? ChangedByName { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    public class ActivityDto
    {
        public string Type { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public int? DurationSeconds { get; set; }
        public string? Result { get; set; }
        public string? InteractionType { get; set; }
    }

    public class MetricsDto
    {
        public int? LeadScore { get; set; }
        public double DaysInPipeline { get; set; }
        public DateTime? LastActivityAt { get; set; }
    }
}
