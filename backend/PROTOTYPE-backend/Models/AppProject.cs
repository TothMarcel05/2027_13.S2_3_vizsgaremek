using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models
{
    public class AppProject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public byte Status { get; set; } = 1;
        public DateTime? Deadline { get; set; }
        public string? OwnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public UserWorkspace? Workspace { get; set; }
        public AppUser? Owner { get; set; }
        public ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    }
}