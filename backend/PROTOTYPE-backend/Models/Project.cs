using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Projektek
/// </summary>
public partial class Project
{
    /// <summary>
    /// Projekt azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Workspace, amelyhez a projekt tartozik
    /// </summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>
    /// Projekt neve
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Projekt leírása
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 1 = planned, 2 = active, 3 = on_hold, 4 = completed, 5 = archived
    /// </summary>
    public sbyte Status { get; set; }

    /// <summary>
    /// Határidő
    /// </summary>
    public DateOnly? Deadline { get; set; }

    /// <summary>
    /// Projekt felelőse
    /// </summary>
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Utolsó módosítás ideje
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

    public virtual ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();

    public virtual AppUser? Owner { get; set; }

    public virtual ICollection<ProjectLabel> ProjectLabels { get; set; } = new List<ProjectLabel>();

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    public virtual ICollection<StorageNode> StorageNodes { get; set; } = new List<StorageNode>();

    public virtual Workspace Workspace { get; set; } = null!;
}
