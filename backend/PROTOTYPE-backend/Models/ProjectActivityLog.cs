using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Projekt tevékenységnapló
/// </summary>
public partial class ProjectActivityLog
{
    /// <summary>
    /// Naplóbejegyzés azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Érintett task
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Műveletet végző felhasználó
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Művelet kódja
    /// </summary>
    public string Action { get; set; } = null!;

    /// <summary>
    /// Részletek
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Művelet ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    public virtual ProjectTask? Task { get; set; }

    public virtual AppUser? User { get; set; }
}
