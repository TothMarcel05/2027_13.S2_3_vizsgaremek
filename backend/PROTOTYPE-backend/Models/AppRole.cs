using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Globális szerepkörök
/// </summary>
public partial class AppRole
{
    /// <summary>
    /// Szerepkör azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Szerepkör neve (globálisan egyedi)
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Leírás
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Beépített szerepkör (nem törölhető)
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AppInvitation> AppInvitations { get; set; } = new List<AppInvitation>();

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();

    public virtual ICollection<StorageNode> Nodes { get; set; } = new List<StorageNode>();

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
