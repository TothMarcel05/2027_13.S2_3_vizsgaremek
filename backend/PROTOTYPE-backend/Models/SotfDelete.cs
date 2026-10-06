public interface ISoftDeletable { DateTime? DeletedAt { get; set; } Guid? DeletedBy { get; set; } }

public partial class AppProject : ISoftDeletable { public DateTime? DeletedAt { get; set; } public Guid? DeletedBy { get; set; } }
public partial class ProjectTask : ISoftDeletable { public DateTime? DeletedAt { get; set; } public Guid? DeletedBy { get; set; } }
public partial class StorageNode : ISoftDeletable { public DateTime? DeletedAt { get; set; } public Guid? DeletedBy { get; set; } }
public partial class UserWorkspace : ISoftDeletable { public DateTime? DeletedAt { get; set; } public Guid? DeletedBy { get; set; } }