using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class ProjectTask
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public Guid? AssigneeId { get; set; }

    public Guid? ReporterId { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual AppUser? Assignee { get; set; }

    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

    public virtual AppProject Project { get; set; } = null!;

    public virtual ICollection<ProjectActivityLog> ProjectActivityLogs { get; set; } = new List<ProjectActivityLog>();

    public virtual ICollection<ProjectComment> ProjectComments { get; set; } = new List<ProjectComment>();

    public virtual AppUser? Reporter { get; set; }

    public virtual ICollection<ProjectLabel> Labels { get; set; } = new List<ProjectLabel>();
}
