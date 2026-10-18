using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Fájlok és mappák egyedi címkéi
/// </summary>
public partial class StorageNodeLabel
{
    /// <summary>
    /// Címke azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Kapcsolódó fájl vagy mappa
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// Címke felirata
    /// </summary>
    public string Label { get; set; } = null!;

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    public virtual StorageNode Node { get; set; } = null!;
}
