using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Chat szobák
/// </summary>
public partial class ChatRoom
{
    /// <summary>
    /// Szoba azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Workspace
    /// </summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>
    /// Projekt
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Szoba neve
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 1 = direct, 2 = group, 3 = project_channel
    /// </summary>
    public sbyte Type { get; set; }

    /// <summary>
    /// Privát-e
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Létrehozó
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual ICollection<ChatRoomMember> ChatRoomMembers { get; set; } = new List<ChatRoomMember>();

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual Project? Project { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;
}
