namespace PROTOTYPE_backend.DTOs.Workspace
{
    public record SelectWorkspaceResponseDto(
        string AccessToken,
        Guid WorkspaceId,
        string WorkspaceName,
        List<string> Roles,
        List<string> Permissions
    );
}
