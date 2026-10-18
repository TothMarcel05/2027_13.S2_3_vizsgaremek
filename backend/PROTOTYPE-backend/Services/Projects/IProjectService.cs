using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Projects
{
    public interface IProjectService
    {
        Task<ProjectTask> CreateTaskAsync(Guid projectId, string title, string? description, byte priority, DateTime? dueDate, Guid? assigneeId );
        Task<bool> UpdateTaskStatusAsync(Guid taskId, byte newStatus);
    }
}
