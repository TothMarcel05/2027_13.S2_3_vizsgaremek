using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models
{
    public class ProjectTask
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public byte Status { get; set; } = 1; 
        public byte Priority { get; set; } = 2;
        public Guid? AssigneeRoleId { get; set; }
        public string? AssigneeUserId { get; set; }
        public string? AssigneeId { get => AssigneeUserId; set => AssigneeUserId = value; }
        public string? ReporterId { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public AppProject? Project { get; set; }
        public Role? AssigneeRole { get; set; }
        public AppUser? Assignee { get; set; }
        public AppUser? Reporter { get; set; }
        public ICollection<ProjectComment> ProjectComments { get; set; } = new List<ProjectComment>();
    }
}