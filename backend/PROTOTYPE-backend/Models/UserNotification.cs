using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class UserNotification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
