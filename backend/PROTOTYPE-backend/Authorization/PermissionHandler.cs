using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Authorization;
using PROTOTYPE_backend.Data;
using System.Security.Claims;
using System.Text.Json;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _db;

    public PermissionHandler(IHttpContextAccessor httpContextAccessor, AppDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // 1. Rendszeradminisztrátor ellenőrzése
        var globalRoleClaim = context.User.FindFirstValue(ClaimTypes.Role);
        if (globalRoleClaim == "2" || globalRoleClaim == "SystemAdmin")
        {
            context.Succeed(requirement);
            return;
        }

        // 2. JWT Claim kinyerése
        var permissionsJson = context.User.FindFirstValue("all_roles_permissions");
        if (string.IsNullOrEmpty(permissionsJson))
        {
            return;
        }

        try
        {
            var rolesAndPermissions = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(permissionsJson);
            if (rolesAndPermissions == null) return;

            var httpContext = _httpContextAccessor.HttpContext;
            var activeWorkspaceId = context.User.FindFirstValue("active_workspace_id");

            // Az erőforrás kontextusának meghatározása: projekt (route/fejléc) vagy feladat (route) vagy munkaterület
            string? projectKey = httpContext?.GetRouteValue("projectId")?.ToString()
                                 ?? httpContext?.Request.Headers["X-Project-Id"].FirstOrDefault();
            string? resourceWorkspaceId = httpContext?.GetRouteValue("workspaceId")?.ToString()
                                          ?? httpContext?.Request.Headers["X-Workspace-Id"].FirstOrDefault();

            if (resourceWorkspaceId == null && Guid.TryParse(projectKey, out var projectGuid))
            {
                resourceWorkspaceId = (await _db.Projects.AsNoTracking()
                    .Where(p => p.Id == projectGuid)
                    .Select(p => (Guid?)p.WorkspaceId)
                    .FirstOrDefaultAsync())?.ToString();
            }

            if (resourceWorkspaceId == null && projectKey == null
                && Guid.TryParse(httpContext?.GetRouteValue("taskId")?.ToString(), out var taskGuid))
            {
                var task = await _db.ProjectTasks.AsNoTracking()
                    .Where(t => t.Id == taskGuid)
                    .Select(t => new { t.ProjectId, WorkspaceId = (Guid?)t.Project.WorkspaceId })
                    .FirstOrDefaultAsync();
                if (task != null)
                {
                    projectKey = task.ProjectId.ToString();
                    resourceWorkspaceId = task.WorkspaceId?.ToString();
                }
            }

            // Projekt-szintű jogosultság (a token csak az aktív munkaterület projektjeit tartalmazza)
            if (!string.IsNullOrEmpty(projectKey)
                && rolesAndPermissions.TryGetValue(projectKey, out var projectPermissions)
                && projectPermissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
                return;
            }

            // Munkaterület-szintű jogosultság: csak arra a munkaterületre érvényes, amelyhez az erőforrás tartozik.
            // Ha az erőforrás munkaterülete nem azonosítható (pl. nem létező id), az aktív munkaterületre esünk vissza,
            // így a végpont a megfelelő 404-et adja.
            var effectiveWorkspaceId = resourceWorkspaceId ?? activeWorkspaceId;
            if (!string.IsNullOrEmpty(effectiveWorkspaceId)
                && rolesAndPermissions.TryGetValue(effectiveWorkspaceId, out var wsPermissions)
                && wsPermissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            }
        }
        catch
        {
            // Hibás JSON esetén elutasítjuk
        }
    }
}
