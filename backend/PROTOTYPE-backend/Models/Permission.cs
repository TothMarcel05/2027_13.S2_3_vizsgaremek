using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Jogosultságok katalógusa
/// </summary>
public partial class Permission
{
    /// <summary>
    /// Jogosultság azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gépi kód, pl. workspace.manage, project.task.create
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// 1 = workspace, 2 = project
    /// </summary>
    public sbyte Scope { get; set; }

    /// <summary>
    /// Olvasható név
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Leírás
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AppRole> Roles { get; set; } = new List<AppRole>();
}
