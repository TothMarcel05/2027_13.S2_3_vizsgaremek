using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Felhasználói értesítések
/// </summary>
public partial class UserNotification
{
    /// <summary>
    /// Értesítés azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Címzett felhasználó
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Szöveg
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Elolvasta-e
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
