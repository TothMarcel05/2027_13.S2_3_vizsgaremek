using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class StorageTag
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = null!;

    public string Color { get; set; } = null!;

    public virtual ICollection<StorageTagRbac> StorageTagRbacs { get; set; } = new List<StorageTagRbac>();

    public virtual UserWorkspace Workspace { get; set; } = null!;

    public virtual ICollection<StorageNode> Nodes { get; set; } = new List<StorageNode>();
}
