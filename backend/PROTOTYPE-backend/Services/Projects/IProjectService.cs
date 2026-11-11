using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.DTOs.Project;

namespace PROTOTYPE_backend.Services.Projects
{
    public interface IProjectService
    {
        Task<ProjectDataDto?> GetProjectByIdAsync(Guid projectId);
        Task<List<ProjectTaskListDto>> GetProjectTasksAsync(Guid projectId, sbyte? status = null, byte? priority = null);
        Task<ProjectTaskResponseDto> GetProjectTaskAsync(Guid taskId);
        Task<List<ProjectResponseDto>> GetWorkspaceByIdAsync(Guid workspaceId);
        Task<ProjectTask> CreateTaskAsync(Guid projectId, string title, string? description, byte priority, DateTime? dueDate, Guid? assigneeId );
        Task<bool> UpdateTaskStatusAsync(Guid taskId, byte newStatus);
    }
}
