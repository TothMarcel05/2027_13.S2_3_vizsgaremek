using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Task kommentek
/// </summary>
public partial class ProjectComment
{
    /// <summary>
    /// Komment azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Task
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Szerző
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Szöveg
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    public virtual ProjectTask Task { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;
}
