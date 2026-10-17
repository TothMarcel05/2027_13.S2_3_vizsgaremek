namespace PROTOTYPE_backend.DTOs.Workspace;

public record CreateWorkspaceDto(string Name);
public record CreateProjectDto(Guid WorkspaceId, string Name, string Description, DateTime? Deadline);
public record CreateTaskDto(Guid ProjectId, string Title, string Description, string Priority, DateTime? DueDate, Guid? AssigneeId);
public record UpdateTaskStatusDto(Guid TaskId, string Status); 

public record TaskDto(Guid Id, string Title, string Description, string Status, string Priority, DateTime? DueDate, Guid? AssigneeId);
public record ProjectDto(Guid Id, string Name, string Description, string Status, DateTime? Deadline, List<TaskDto> Tasks);
public record WorkspaceDto(Guid Id, string Name, List<ProjectDto> Projects);