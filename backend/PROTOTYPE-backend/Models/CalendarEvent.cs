using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class CalendarEvent
{
    public Guid Id { get; set; }

    public Guid CalendarId { get; set; }

    public Guid? TaskId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Location { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsAllDay { get; set; }

    public Guid? CreatedBy { get; set; }

    public virtual Calendar Calendar { get; set; } = null!;

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual ProjectTask? Task { get; set; }
}
