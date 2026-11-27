using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Visszavont JWT tokenek feketelistája
/// </summary>
public partial class JwtBlacklistedToken
{
    /// <summary>
    /// Bejegyzés azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// JWT jti claim
    /// </summary>
    public string Jti { get; set; } = null!;

    /// <summary>
    /// Felhasználó
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// A token lejárata
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Feketelistára kerülés ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
