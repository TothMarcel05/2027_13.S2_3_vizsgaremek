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
            string? currentContextId = httpContext?.GetRouteValue("workspaceId")?.ToString()
                                     ?? httpContext?.GetRouteValue("projectId")?.ToString()
                                     ?? httpContext?.Request.Headers["X-Workspace-Id"].FirstOrDefault();

            var rolesAndPermissions = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(permissionsJson);

            if (rolesAndPermissions != null)
            {
                // HA van kontextus (pl. workspace/project ID az URL-ben):
                if (!string.IsNullOrEmpty(currentContextId) && rolesAndPermissions.TryGetValue(currentContextId, out var contextPermissions))
                {
                    // Csak az ADOTT kontextushoz tartozó jogosultságokat ellenőrizzük!
                    if (contextPermissions.Contains(requirement.Permission))
                    {
                        context.Succeed(requirement);
                    }
                }
                else
                {
                    // HA globális a kérés (nincs konkrét workspace a művelet mögött), 
                    // vagy fallback képpen nézzük az összeset:
                    var hasPermission = rolesAndPermissions.Values
                        .SelectMany(permissionsList => permissionsList)
                        .Contains(requirement.Permission);

                    if (hasPermission)
                    {
                        context.Succeed(requirement);
                    }
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