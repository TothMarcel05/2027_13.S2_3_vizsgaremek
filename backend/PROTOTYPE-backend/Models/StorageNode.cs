using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Fájlok és mappák fastruktúrája
/// </summary>
public partial class StorageNode
{
    /// <summary>
    /// Node azonosító (UUID)
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
    /// Szülő mappa (NULL = gyökér)
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Fájl vagy mappa neve
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// 1 = folder, 2 = file
    /// </summary>
    public sbyte Type { get; set; }

    /// <summary>
    /// MIME típus
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Fájl fizikai helye
    /// </summary>
    public string? StoragePath { get; set; }

    /// <summary>
    /// Méret bájtban
    /// </summary>
    public ulong SizeBytes { get; set; }

    /// <summary>
    /// Létrehozó felhasználó
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Utolsó módosító
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Törlés ideje
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Utolsó módosítás ideje
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ChatMessageAttachment> ChatMessageAttachments { get; set; } = new List<ChatMessageAttachment>();

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual ICollection<StorageNode> InverseParent { get; set; } = new List<StorageNode>();

    public virtual StorageNode? Parent { get; set; }

    public virtual Project? Project { get; set; }

    public virtual ICollection<StorageNodeLabel> StorageNodeLabels { get; set; } = new List<StorageNodeLabel>();

    public virtual AppUser? UpdatedByNavigation { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;

    public virtual ICollection<AppRole> Roles { get; set; } = new List<AppRole>();
}
