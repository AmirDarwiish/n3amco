using System;
using System.Collections.Generic;

namespace CourseCenter.Api.Leads.DTOs
{
    public class PipelineLeadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? CompanyName { get; set; }
        public decimal? DealValue { get; set; }
        public string? Currency { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public string? LastActivityType { get; set; }
        public AssignedUserDto? AssignedUser { get; set; }
        public List<string>? Tags { get; set; }
    }

    public class AssignedUserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
    }

    public class PipelineStageDto
    {
        public int StageId { get; set; }
        public string StageName { get; set; } = null!;
        public int TotalLeads { get; set; }
        public decimal? TotalValue { get; set; }
        public List<PipelineLeadDto> Leads { get; set; } = new();
    }
}
