using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models
{
    public class StorageNode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte Type { get; set; } = 2;
        public string? MimeType { get; set; }
        public string? StoragePath { get; set; }
        public ulong SizeBytes { get; set; } = 0;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; } 

        public UserWorkspace? Workspace { get; set; }
        public AppProject? Project { get; set; }
        public StorageNode? Parent { get; set; }
        public AppUser? Creator { get; set; }

        public ICollection<StorageNode> InverseParent { get; set; } = new List<StorageNode>();
        public ICollection<StorageNodeLabel> StorageNodeLabels { get; set; } = new List<StorageNodeLabel>();
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}