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

            // Meghívó célja: kizárólag a cél (projekt vagy annak munkaterülete) számít,
            // sem a fejlécek, sem az aktív munkaterület nem helyettesítheti.
            if (context.Resource is InvitationTargetResource target)
            {
                var targetWorkspaceId = target.Type == 2
                    ? await _db.Projects.AsNoTracking()
                        .Where(p => p.Id == target.TargetId)
                        .Select(p => (Guid?)p.WorkspaceId)
                        .FirstOrDefaultAsync()
                    : target.TargetId;

                if (targetWorkspaceId != null
                    && (HasPermission(target.TargetId) || HasPermission(targetWorkspaceId.Value)))
                {
                    context.Succeed(requirement);
                }
                return;

                bool HasPermission(Guid key) =>
                    rolesAndPermissions.TryGetValue(key.ToString(), out var permissions)
                    && permissions.Contains(requirement.Permission);
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var activeWorkspaceId = context.User.FindFirstValue("active_workspace_id");

            // Munkaterülethez tartozó elem: a jogosultság csak az elem saját munkaterületére érvényes.
            if (context.Resource is WorkspaceOwnedResource owned)
            {
                Guid? ownerWorkspaceId = null;

                if (owned.EntityName == "Workspace")
                {
                    var workspace = await _db.Workspaces.AsNoTracking()
                        .Where(w => w.Id == owned.EntityId)
                        .Select(w => new { w.Id, w.OwnerId })
                        .FirstOrDefaultAsync();

                    // A tulajdonos a törölt munkaterületébe nem tud belépni, de visszaállíthatja.
                    if (workspace != null && workspace.OwnerId.ToString() == context.User.FindFirstValue(ClaimTypes.NameIdentifier))
                    {
                        context.Succeed(requirement);
                        return;
                    }
                    ownerWorkspaceId = workspace?.Id;
                }
                else if (owned.EntityName == "Project")
                {
                    ownerWorkspaceId = await _db.Projects.AsNoTracking()
                        .Where(p => p.Id == owned.EntityId)
                        .Select(p => (Guid?)p.WorkspaceId)
                        .FirstOrDefaultAsync();
                }
                else if (owned.EntityName == "StorageNode")
                {
                    ownerWorkspaceId = await _db.StorageNodes.AsNoTracking()
                        .Where(n => n.Id == owned.EntityId)
                        .Select(n => (Guid?)n.WorkspaceId)
                        .FirstOrDefaultAsync();
                }

                // Nem létező elemnél az aktív munkaterületre esünk vissza, így a végpont a megfelelő hibát adja.
                var requiredKey = ownerWorkspaceId?.ToString() ?? activeWorkspaceId;
                if (!string.IsNullOrEmpty(requiredKey)
                    && rolesAndPermissions.TryGetValue(requiredKey, out var ownedPermissions)
                    && ownedPermissions.Contains(requirement.Permission))
                {
                    context.Succeed(requirement);
                }
                return;
            }

            // Az erőforrás kontextusának meghatározása: projekt (route) vagy feladat (route) vagy munkaterület (route).
            // A kliens által küldött fejlécekben nem bízunk, azok nem határozhatják meg a kontextust.
            string? projectKey = httpContext?.GetRouteValue("projectId")?.ToString();
            string? resourceWorkspaceId = httpContext?.GetRouteValue("workspaceId")?.ToString();

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
