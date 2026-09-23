using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class StorageFileVersion
{
    public Guid Id { get; set; }

    public Guid NodeId { get; set; }

    public string FilePath { get; set; } = null!;

    public int VersionNumber { get; set; }

    public ulong SizeBytes { get; set; }

    public Guid? UploadedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual StorageNode Node { get; set; } = null!;

    public virtual AppUser? UploadedByNavigation { get; set; }
}
