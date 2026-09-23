using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class Permission
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
