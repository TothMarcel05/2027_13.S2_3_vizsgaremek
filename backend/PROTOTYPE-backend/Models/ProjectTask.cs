using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Projekt taskok
/// </summary>
public partial class ProjectTask
{
    /// <summary>
    /// Task azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Projekt, amelyhez a task tartozik
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Task címe
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Task leírása
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 1 = planning, 2 = on_going, 3 = ready
    /// </summary>
    public sbyte Status { get; set; }

    /// <summary>
    /// 1 = low, 2 = medium, 3 = high, 4 = urgent
    /// </summary>
    public sbyte Priority { get; set; }

    /// <summary>
    /// Szerepkör, amire a task ki van hirdetve
    /// </summary>
    public Guid? AssigneeRoleId { get; set; }

    /// <summary>
    /// Konkrét felhasználó, aki lefoglalta a feladatot
    /// </summary>
    public Guid? AssigneeUserId { get; set; }

    /// <summary>
    /// Létrehozó felhasználó
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Határidő
    /// </summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Utolsó módosítás ideje
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual AppRole? AssigneeRole { get; set; }

    public virtual AppUser? AssigneeUser { get; set; }

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<ProjectActivityLog> ProjectActivityLogs { get; set; } = new List<ProjectActivityLog>();

    public virtual ICollection<ProjectComment> ProjectComments { get; set; } = new List<ProjectComment>();

    public virtual ICollection<ProjectLabel> Labels { get; set; } = new List<ProjectLabel>();
}
