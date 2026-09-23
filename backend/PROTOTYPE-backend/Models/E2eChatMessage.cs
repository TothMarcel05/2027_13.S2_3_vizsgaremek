using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class E2eChatMessage
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Guid? SenderId { get; set; }

    public string SenderDeviceId { get; set; } = null!;

    public Guid? ParentId { get; set; }

    public string Ciphertext { get; set; } = null!;

    public string NonceOrIv { get; set; } = null!;

    public int KeyVersion { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ChatMessageAttachment> ChatMessageAttachments { get; set; } = new List<ChatMessageAttachment>();

    public virtual ICollection<ChatRoomMember> ChatRoomMembers { get; set; } = new List<ChatRoomMember>();

    public virtual ICollection<E2eChatMessage> InverseParent { get; set; } = new List<E2eChatMessage>();

    public virtual E2eChatMessage? Parent { get; set; }

    public virtual ChatRoom Room { get; set; } = null!;

    public virtual AppUser? Sender { get; set; }
}
