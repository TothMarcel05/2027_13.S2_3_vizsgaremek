using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Egyszeri 2FA kódok
/// </summary>
public partial class User2faCode
{
    /// <summary>
    /// Kód azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Felhasználó
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Kód hash
    /// </summary>
    public string CodeHash { get; set; } = null!;

    /// <summary>
    /// Sikertelen próbálkozások
    /// </summary>
    public byte FailedAttempts { get; set; }

    /// <summary>
    /// Max próbálkozás
    /// </summary>
    public byte MaxAttempts { get; set; }

    /// <summary>
    /// Lejárat ideje
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Felhasználták-e
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
