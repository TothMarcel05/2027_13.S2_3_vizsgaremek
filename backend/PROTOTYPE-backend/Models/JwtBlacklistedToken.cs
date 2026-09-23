using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class JwtBlacklistedToken
{
    public Guid Id { get; set; }

    public string Jti { get; set; } = null!;

    public Guid UserId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
