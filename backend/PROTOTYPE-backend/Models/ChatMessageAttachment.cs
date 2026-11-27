using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Chat üzenet mellékletek
/// </summary>
public partial class ChatMessageAttachment
{
    /// <summary>
    /// Melléklet azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Üzenet
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Kapcsolódó storage node
    /// </summary>
    public Guid? StorageNodeId { get; set; }

    /// <summary>
    /// Fájl URL
    /// </summary>
    public string FileUrl { get; set; } = null!;

    /// <summary>
    /// Fájl neve
    /// </summary>
    public string FileName { get; set; } = null!;

    public virtual ChatMessage Message { get; set; } = null!;

    public virtual StorageNode? StorageNode { get; set; }
}
