using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Projekt címkék
/// </summary>
public partial class ProjectLabel
{
    /// <summary>
    /// Címke azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Projekt
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Címke neve
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Szín hex kódja
    /// </summary>
    public string Color { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
}
