using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Workspace tagságok és szerepkörük
/// </summary>
public partial class WorkspaceMember
{
    /// <summary>
    /// Workspace
    /// </summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>
    /// Tag
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// A tag szerepköre a workspace-ben
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Csatlakozás ideje
    /// </summary>
    public DateTime? JoinedAt { get; set; }

    public virtual AppRole Role { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;

    public virtual Workspace Workspace { get; set; } = null!;
}
