using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class StorageTagRbac
{
    public Guid TagId { get; set; }

    public Guid RoleId { get; set; }

    public bool? CanRead { get; set; }

    public bool CanWrite { get; set; }

    public bool CanDelete { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual StorageTag Tag { get; set; } = null!;
}
