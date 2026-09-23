using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class ProjectLabel
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string Name { get; set; } = null!;

    public string Color { get; set; } = null!;

    public virtual AppProject Project { get; set; } = null!;

    public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
}
