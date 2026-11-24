using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using PROTOTYPE_backend.Authorization;
using System.Security.Claims;
using System.Text.Json;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Injektáljuk az HttpContextAccessor-t a kérés adatainak eléréséhez
    public PermissionHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // 1. Rendszeradminisztrátor ellenőrzése
        var globalRoleClaim = context.User.FindFirstValue(ClaimTypes.Role);
        if (globalRoleClaim == "2" || globalRoleClaim == "SystemAdmin")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. JWT Claim kinyerése
        var permissionsJson = context.User.FindFirstValue("all_roles_permissions");
        if (string.IsNullOrEmpty(permissionsJson))
        {
            return Task.CompletedTask;
        }

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // Kinyerjük a workspaceId-t vagy projectId-t az Route Data-ból vagy Query/Header-ből
            string? currentContextId = httpContext?.GetRouteValue("projectId")?.ToString()
                         ?? httpContext?.GetRouteValue("workspaceId")?.ToString()
                         ?? httpContext?.Request.Headers["X-Project-Id"].FirstOrDefault()
                         ?? httpContext?.Request.Headers["X-Workspace-Id"].FirstOrDefault()
                         ?? context.User.FindFirstValue("active_workspace_id");

            var rolesAndPermissions = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(permissionsJson);

            if (rolesAndPermissions != null)
            {
                // 1. Elsődleges kontextus (pl. projectId vagy route workspaceId) ellenőrzése
                if (!string.IsNullOrEmpty(currentContextId) &&
                    rolesAndPermissions.TryGetValue(currentContextId, out var contextPermissions) &&
                    contextPermissions.Contains(requirement.Permission))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }

                // 2. Fallback: aktív munkaterület jogosultságainak ellenőrzése (pl. Workspace Owner/Admin projekt-műveleteknél)
                var activeWorkspaceId = context.User.FindFirstValue("active_workspace_id");
                if (!string.IsNullOrEmpty(activeWorkspaceId) &&
                    activeWorkspaceId != currentContextId &&
                    rolesAndPermissions.TryGetValue(activeWorkspaceId, out var wsPermissions) &&
                    wsPermissions.Contains(requirement.Permission))
                {
                    context.Succeed(requirement);
                }
            }
        }
        catch
        {
            // Hibás JSON esetén elutasítjuk
        }

        return Task.CompletedTask;
    }
}