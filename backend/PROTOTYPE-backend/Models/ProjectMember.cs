using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Projekttagságok
/// </summary>
public partial class ProjectMember
{
    /// <summary>
    /// Projekt azonosító
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// A projekt workspace-e
    /// </summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>
    /// Felhasználó azonosító
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// A tag szerepköre a projektben
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Jelzi, hogy a felhasználó a felettes workspace-nek is tagja-e
    /// </summary>
    public bool? IsWorkspaceMember { get; set; }

    /// <summary>
    /// Hozzárendelés ideje
    /// </summary>
    public DateTime? AssignedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual AppRole Role { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;

    public virtual Workspace Workspace { get; set; } = null!;
}
