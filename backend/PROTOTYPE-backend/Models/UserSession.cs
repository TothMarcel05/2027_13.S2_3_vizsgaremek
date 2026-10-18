using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Felhasználói munkamenetek
/// </summary>
public partial class UserSession
{
    /// <summary>
    /// Munkamenet azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Felhasználó
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Refresh token hash
    /// </summary>
    public string RefreshTokenHash { get; set; } = null!;

    /// <summary>
    /// Rotációs felváltó session
    /// </summary>
    public Guid? ReplacedBySessionId { get; set; }

    /// <summary>
    /// Eszköz azonosító
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Böngésző / OS infó
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// IP cím
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Visszavonták-e
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Lejárat ideje
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Utolsó módosítás ideje
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<UserSession> InverseReplacedBySession { get; set; } = new List<UserSession>();

    public virtual UserSession? ReplacedBySession { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
