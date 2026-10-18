using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Naptár események
/// </summary>
public partial class CalendarEvent
{
    /// <summary>
    /// Esemény azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Projekt
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Esemény címe
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Leírás
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Helyszín
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Kezdés
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Befejezés
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Egész napos-e
    /// </summary>
    public bool IsAllDay { get; set; }

    /// <summary>
    /// Létrehozó
    /// </summary>
    public Guid? CreatedBy { get; set; }

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual Project? Project { get; set; }
}
