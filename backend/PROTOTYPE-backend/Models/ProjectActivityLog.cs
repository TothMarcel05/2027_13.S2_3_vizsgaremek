using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class ProjectActivityLog
{
    public Guid Id { get; set; }

    public Guid? TaskId { get; set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string? Details { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ProjectTask? Task { get; set; }

    public virtual AppUser? User { get; set; }
}
