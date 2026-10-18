using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models
{
    public class UserWorkspace
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public long StorageQuotaBytes { get; set; } = 26843545600; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public AppUser? Owner { get; set; }
        public ICollection<AppProject> AppProjects { get; set; } = new List<AppProject>();
        public ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
    }
}