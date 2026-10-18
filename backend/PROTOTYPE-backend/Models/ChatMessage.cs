using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Chat üzenetek
/// </summary>
public partial class ChatMessage
{
    /// <summary>
    /// Üzenet azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Szoba
    /// </summary>
    public Guid RoomId { get; set; }

    /// <summary>
    /// Feladó
    /// </summary>
    public Guid? SenderId { get; set; }

    /// <summary>
    /// Szülő üzenet
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Üzenet szövege
    /// </summary>
    public string MessageText { get; set; } = null!;

    /// <summary>
    /// Küldés ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ChatMessageAttachment> ChatMessageAttachments { get; set; } = new List<ChatMessageAttachment>();

    public virtual ICollection<ChatMessage> InverseParent { get; set; } = new List<ChatMessage>();

    public virtual ChatMessage? Parent { get; set; }

    public virtual ChatRoom Room { get; set; } = null!;

    public virtual AppUser? Sender { get; set; }
}
