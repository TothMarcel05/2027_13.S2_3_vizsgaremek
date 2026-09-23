using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class Calendar
{
    public Guid Id { get; set; }

    public Guid? WorkspaceId { get; set; }

    public Guid? ProjectId { get; set; }

    public Guid? UserId { get; set; }

    public string Name { get; set; } = null!;

    public string Color { get; set; } = null!;

    public string Type { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

    public virtual AppProject? Project { get; set; }

    public virtual AppUser? User { get; set; }

    public virtual UserWorkspace? Workspace { get; set; }
}
