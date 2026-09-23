using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class Role
{
    public Guid Id { get; set; }

    public Guid? WorkspaceId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsSystem { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<StorageTagRbac> StorageTagRbacs { get; set; } = new List<StorageTagRbac>();

    public virtual UserWorkspace? Workspace { get; set; }

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
}
