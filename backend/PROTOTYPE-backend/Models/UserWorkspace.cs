using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class UserWorkspace
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid OwnerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AppProject> AppProjects { get; set; } = new List<AppProject>();

    public virtual ICollection<Calendar> Calendars { get; set; } = new List<Calendar>();

    public virtual ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();

    public virtual AppUser Owner { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<StorageNode> StorageNodes { get; set; } = new List<StorageNode>();

    public virtual ICollection<StorageTag> StorageTags { get; set; } = new List<StorageTag>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
}
