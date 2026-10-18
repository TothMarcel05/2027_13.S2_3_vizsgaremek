using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Rendszerszintű enum számszerű értékek szótára
/// </summary>
public partial class EnumLookup
{
    /// <summary>
    /// Elsődleges kulcs
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Kategória neve, pl. global_role, project_status
    /// </summary>
    public string Category { get; set; } = null!;

    /// <summary>
    /// Számkód (1, 2, 3...)
    /// </summary>
    public sbyte Code { get; set; }

    /// <summary>
    /// Megjelenített név / kód név
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Opcionális leírás
    /// </summary>
    public string? Description { get; set; }
}
