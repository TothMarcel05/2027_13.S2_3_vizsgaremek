using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class E2eUserKey
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string DeviceId { get; set; } = null!;

    public string IdentityKey { get; set; } = null!;

    public string SignedPrekey { get; set; } = null!;

    public string PrekeySignature { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
