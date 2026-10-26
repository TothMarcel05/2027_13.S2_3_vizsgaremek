using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Workspace-ek (a legfelső szintű szervezeti egység)
/// </summary>
public partial class Workspace
{
    /// <summary>
    /// Workspace azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Workspace neve
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Workspace leírása
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Tulajdonos felhasználó
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Tárhelykvóta bájtban (alapértelmezett: 25 GB)
    /// </summary>
    public ulong StorageQuotaBytes { get; set; }

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();

    public virtual AppUser Owner { get; set; } = null!;

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<StorageNode> StorageNodes { get; set; } = new List<StorageNode>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
}
