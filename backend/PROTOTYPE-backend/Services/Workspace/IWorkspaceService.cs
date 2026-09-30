using PROTOTYPE_backend.DTOs.Workspace;

namespace PROTOTYPE_backend.Services.Workspace
{
    public interface IWorkspaceService
    {
        Task<SelectWorkspaceResponseDto> SelectWorkspaceAsync(Guid userId, Guid workspaceId);
    }
}
