using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class StorageNode
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid? ProjectId { get; set; }

    public Guid? ParentId { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? MimeType { get; set; }

    public ulong? SizeBytes { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ChatMessageAttachment> ChatMessageAttachments { get; set; } = new List<ChatMessageAttachment>();

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual ICollection<StorageNode> InverseParent { get; set; } = new List<StorageNode>();

    public virtual StorageNode? Parent { get; set; }

    public virtual AppProject? Project { get; set; }

    public virtual ICollection<StorageFileVersion> StorageFileVersions { get; set; } = new List<StorageFileVersion>();

    public virtual UserWorkspace Workspace { get; set; } = null!;

    public virtual ICollection<StorageTag> Tags { get; set; } = new List<StorageTag>();
}
