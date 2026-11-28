using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PROTOTYPE_backend.Authorization
{
    // Erőforrás-alapú ellenőrzéshez: egy munkaterülethez tartozó elem ("Workspace", "Project", "StorageNode")
    public record WorkspaceOwnedResource(string EntityName, Guid EntityId);

    public static class WorkspaceOwnedResourceExtensions
    {
        /// <summary>A jogosultságot az elem saját munkaterületén követeli meg, különben 403.</summary>
        public static async Task EnsurePermissionOnAsync(this IAuthorizationService authorizationService,
            ClaimsPrincipal user, string permission, string entityName, Guid entityId)
        {
            var result = await authorizationService.AuthorizeAsync(user, new WorkspaceOwnedResource(entityName, entityId), permission);
            if (!result.Succeeded)
                throw new UnauthorizedAccessException("Nincs jogosultságod a művelet végrehajtásához.");
        }
    }
}
