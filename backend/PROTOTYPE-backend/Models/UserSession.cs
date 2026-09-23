using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class UserSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string RefreshTokenHash { get; set; } = null!;

    public Guid? ReplacedBySessionId { get; set; }

    public string? DeviceInfo { get; set; }

    public string? IpAddress { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<UserSession> InverseReplacedBySession { get; set; } = new List<UserSession>();

    public virtual UserSession? ReplacedBySession { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
