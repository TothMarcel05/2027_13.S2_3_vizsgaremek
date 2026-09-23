using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class ProjectMember
{
    public Guid ProjectId { get; set; }

    public Guid UserId { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual AppProject Project { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
