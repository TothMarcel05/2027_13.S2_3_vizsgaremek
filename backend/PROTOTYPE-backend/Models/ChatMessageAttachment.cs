using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class ChatMessageAttachment
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public Guid? StorageNodeId { get; set; }

    public string FileUrl { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public virtual E2eChatMessage Message { get; set; } = null!;

    public virtual StorageNode? StorageNode { get; set; }
}
