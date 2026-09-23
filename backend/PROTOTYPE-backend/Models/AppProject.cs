using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class AppProject
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly? Deadline { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Calendar> Calendars { get; set; } = new List<Calendar>();

    public virtual ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();

    public virtual ICollection<ProjectLabel> ProjectLabels { get; set; } = new List<ProjectLabel>();

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    public virtual ICollection<StorageNode> StorageNodes { get; set; } = new List<StorageNode>();

    public virtual UserWorkspace Workspace { get; set; } = null!;
}
