using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class ChatRoom
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid? ProjectId { get; set; }

    public string? Name { get; set; }

    public string Type { get; set; } = null!;

    public bool IsPrivate { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ChatRoomMember> ChatRoomMembers { get; set; } = new List<ChatRoomMember>();

    public virtual ICollection<E2eChatMessage> E2eChatMessages { get; set; } = new List<E2eChatMessage>();

    public virtual ICollection<E2eRoomKey> E2eRoomKeys { get; set; } = new List<E2eRoomKey>();

    public virtual AppProject? Project { get; set; }

    public virtual UserWorkspace Workspace { get; set; } = null!;
}
