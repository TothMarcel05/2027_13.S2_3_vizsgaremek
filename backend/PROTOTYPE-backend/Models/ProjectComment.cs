using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class ProjectComment
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public Guid UserId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ProjectTask Task { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;
}
