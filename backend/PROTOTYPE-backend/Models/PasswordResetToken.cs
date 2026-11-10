using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Jelszóvisszaállító kérések és tokenek
/// </summary>
public partial class PasswordResetToken
{
    /// <summary>
    /// Azonosító
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Felhasználó
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Token hash
    /// </summary>
    public string TokenHash { get; set; } = null!;

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
