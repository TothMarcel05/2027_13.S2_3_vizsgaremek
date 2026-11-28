using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;

namespace PROTOTYPE_backend.Authorization
{
    /// <summary>
    /// Szerepkör-kiosztási szabály: a hívó csak olyan szerepkört kezelhet, amelynek minden jogosultságával
    /// maga is rendelkezik az adott munkaterületen (projekt esetén a projektszintű jogosultságai is számítanak).
    /// A SystemAdmin mentességét a hívó oldal kezeli.
    /// </summary>
    public static class RoleGrantRules
    {
        public static async Task EnsureCallerCoversRolesAsync(
            this AppDbContext db, Guid callerId, Guid workspaceId, Guid? projectId, params Guid[] roleIds)
        {
            var callerPermissions = await db.WorkspaceMembers
                .Where(m => m.WorkspaceId == workspaceId && m.UserId == callerId)
                .SelectMany(m => m.Role.Permissions.Select(p => p.Code))
                .ToListAsync();

            if (projectId != null)
            {
                callerPermissions.AddRange(await db.ProjectMembers
                    .Where(m => m.ProjectId == projectId && m.UserId == callerId)
                    .SelectMany(m => m.Role.Permissions.Select(p => p.Code))
                    .ToListAsync());
            }

            foreach (var roleId in roleIds)
            {
                var role = await db.AppRoles
                    .AsNoTracking()
                    .Where(r => r.Id == roleId)
                    .Select(r => new { Permissions = r.Permissions.Select(p => p.Code).ToList() })
                    .FirstOrDefaultAsync()
                    ?? throw new ArgumentException("A megadott szerepkör nem létezik.");

                if (role.Permissions.Except(callerPermissions).Any())
                    throw new UnauthorizedAccessException("Nem kezelhetsz olyan szerepkört, amely a sajátodnál több jogosultságot tartalmaz.");
            }
        }
    }
}
