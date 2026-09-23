using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class WorkspaceMember
{
    public Guid WorkspaceId { get; set; }

    public Guid UserId { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual AppUser User { get; set; } = null!;

    public virtual UserWorkspace Workspace { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
