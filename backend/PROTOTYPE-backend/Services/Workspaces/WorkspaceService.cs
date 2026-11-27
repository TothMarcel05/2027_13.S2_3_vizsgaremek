using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.Services.Token;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PROTOTYPE_backend.Services.Workspaces
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly ILogger<WorkspaceService> _logger;

        public WorkspaceService(AppDbContext context, ITokenService tokenService, ILogger<WorkspaceService> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<List<WorkspaceResponseDto>> GetUserWorkspacesAsync(Guid userId, int page = 1, int limit = 20)
        {
            var userExists = await _context.AppUsers.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                _logger.LogWarning("Munkaterületek lekérdezése sikertelen: nem létező felhasználó. UserId: {UserId}", userId);
                throw new KeyNotFoundException("Nem létezik a felhasználó!");
            }

            return await _context.Workspaces
                .AsNoTracking()
                .Where(w => w.DeletedAt == null && (w.OwnerId == userId || w.WorkspaceMembers.Any(m => m.UserId == userId)))
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(w => new WorkspaceResponseDto(w.Id, w.Name, w.OwnerId))
                .ToListAsync();
        }

        public async Task<WorkspaceDataResponseDto> CreateWorkspaceAsync(Guid userId, CreateWorkspaceDto dto)
        {
            var existingWorkspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.Name == dto.workspaceName && w.OwnerId == userId && w.DeletedAt == null);

            if (existingWorkspace != null)
            {
                _logger.LogWarning("Munkaterület létrehozása elutasítva: duplikált név a felhasználónál. UserId: {UserId}, WorkspaceName: {WorkspaceName}",
                    userId, dto.workspaceName);
                throw new InvalidOperationException("Már létezik ilyen nevű munkaterület ehhez a felhasználóhoz!");
            }

            var now = DateTime.UtcNow;
            var workspace = new Workspace
            {
                Id = Guid.NewGuid(),
                Name = dto.workspaceName,
                OwnerId = userId,
                CreatedAt = now,
                Description = dto.description,
                StorageQuotaBytes = 1073741824 // pl. 1 GB alapértelmezett kvóta
            };

            _context.Workspaces.Add(workspace);

            // Tulajdonos hozzáadása a WorkspaceMembers táblához Owner szerepkörrel
            var ownerRole = await _context.AppRoles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == "Owner");
            if (ownerRole != null)
            {
                _context.WorkspaceMembers.Add(new WorkspaceMember
                {
                    WorkspaceId = workspace.Id,
                    UserId = userId,
                    RoleId = ownerRole.Id,
                    JoinedAt = now
                });
            }

            await _context.SaveChangesAsync();

            var user = await _context.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

            _logger.LogInformation("Új munkaterület létrehozva. WorkspaceId: {WorkspaceId}, OwnerId: {UserId}", workspace.Id, userId);

            return new WorkspaceDataResponseDto(
                workspace.Id, workspace.Name, workspace.OwnerId, user?.Name ?? "Ismeretlen", workspace.StorageQuotaBytes
            );
        }

        public async Task<WorkspaceDataResponseDto?> GetWorkspaceDataAsync(Guid workspaceId)
        {
            var data = await _context.Workspaces
                .AsNoTracking()
                .Where(w => w.Id == workspaceId && w.DeletedAt == null)
                .Select(w => new WorkspaceDataResponseDto(w.Id, w.Name, w.OwnerId, w.Owner.Name, w.StorageQuotaBytes))
                .FirstOrDefaultAsync();

            if (data == null)
            {
                _logger.LogDebug("Munkaterület adatok lekérdezése: nem található rekord. WorkspaceId: {WorkspaceId}", workspaceId);
            }

            return data;
        }

        public async Task<List<WorkspaceProjectResponseDto>> GetWorkspaceProjectsAsync(Guid workspaceId, int page = 1, int limit = 20, sbyte? status = null)
        {
            var query = _context.Projects
                .AsNoTracking()
                .Where(p => p.WorkspaceId == workspaceId && p.DeletedAt == null);

            if (status.HasValue) query = query.Where(p => p.Status == status.Value);

            return await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new WorkspaceProjectResponseDto(p.WorkspaceId, p.Id, p.Name, p.Status, p.Description, p.Deadline))
                .ToListAsync();
        }

        public async Task<List<WorkspaceMemberResponseDto>> GetWorkspaceMembersAsync(Guid workspaceId, int page = 1, int limit = 50, Guid? role = null)
        {
            var query = _context.WorkspaceMembers
                .AsNoTracking()
                .Where(p => p.WorkspaceId == workspaceId);

            if (role.HasValue) query = query.Where(p => p.RoleId == role.Value);

            return await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new WorkspaceMemberResponseDto(p.WorkspaceId, p.UserId, p.User.Name, p.RoleId, p.JoinedAt))
                .ToListAsync();
        }

        public async Task<WorkspaceProjectResponseDto> CreateProjectAsync(Guid workspaceId, CreateProjectDto dto)
        {
            var project = new Project
            {
                WorkspaceId = workspaceId,
                Name = dto.name,
                Description = dto.description,
                Deadline = dto.deadline,
                OwnerId = dto.ownerId,
                Status = 1,
                CreatedAt = DateTime.UtcNow
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Új projekt létrehozva a munkaterületen belül. ProjectId: {ProjectId}, WorkspaceId: {WorkspaceId}, OwnerId: {OwnerId}",
                project.Id, workspaceId, dto.ownerId);

            return new WorkspaceProjectResponseDto(project.WorkspaceId, project.Id, project.Name, project.Status, project.Description, project.Deadline);
        }

        public async Task<string> SwitchWorkspaceAsync(Guid userId, Guid workspaceId)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive == true);
            var workspaceExists = await _context.Workspaces.AnyAsync(w => w.Id == workspaceId && w.DeletedAt == null);
            if (!workspaceExists) throw new KeyNotFoundException("A munkaterület nem található!");

            if (user == null)
            {
                _logger.LogWarning("Munkaterület-váltás sikertelen: érvénytelen vagy törölt felhasználó. UserId: {UserId}, WorkspaceId: {WorkspaceId}",
                    userId, workspaceId);
                throw new UnauthorizedAccessException("Érvénytelen felhasználó!");
            }

            var rolesNPermissions = new Dictionary<string, List<string>>();

            // 1. Workspace-szintű jogosultságok lekérése
            var workspaceMembership = await _context.WorkspaceMembers
                .Where(m => m.UserId == userId && m.WorkspaceId == workspaceId)
                .Select(m => new
                {
                    Permissions = m.Role.Permissions.Select(p => p.Code).ToList()
                })
                .FirstOrDefaultAsync();

            // Ha nem tagja a workspace-nek és nem is SystemAdmin, utasítsuk el
            if (workspaceMembership == null && user.GlobalRole != 2)
            {
                _logger.LogWarning("Jogosulatlan munkaterület-belépési kísérlet! UserId: {UserId}, WorkspaceId: {WorkspaceId}", userId, workspaceId);
                throw new UnauthorizedAccessException("Nincs hozzáférésed ehhez a munkaterülethez!");
            }

            if (workspaceMembership != null)
            {
                rolesNPermissions[workspaceId.ToString()] = workspaceMembership.Permissions.Distinct().ToList();
            }
            else if (user.GlobalRole == 2)
            {
                _logger.LogInformation("SystemAdmin globális jogosultsággal lépett be a munkaterületre. UserId: {UserId}, WorkspaceId: {WorkspaceId}",
                    userId, workspaceId);
            }

            // 2. Projekt-szintű jogosultságok lekérése az adott workspace-en belül
            var projectMemberships = await _context.ProjectMembers
                .Where(pm => pm.UserId == userId && pm.WorkspaceId == workspaceId)
                .Select(pm => new
                {
                    pm.ProjectId,
                    Permissions = pm.Role.Permissions.Select(p => p.Code).ToList()
                })
                .ToListAsync();

            foreach (var projectGroup in projectMemberships.GroupBy(pm => pm.ProjectId))
            {
                rolesNPermissions[projectGroup.Key.ToString()] = projectGroup
                    .SelectMany(pm => pm.Permissions)
                    .Distinct()
                    .ToList();
            }

            _logger.LogDebug("Munkaterület token kiállítva. UserId: {UserId}, WorkspaceId: {WorkspaceId}, ProjektekSzáma: {ProjectCount}",
                userId, workspaceId, rolesNPermissions.Count);

            return _tokenService.GenerateWorkspaceToken(user, workspaceId, rolesNPermissions);
        }

        public async Task<bool> DeleteWorkspaceAsync(Guid workspaceId)
        {
            var workspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId && w.DeletedAt == null);
            if (workspace == null)
            {
                _logger.LogWarning("Munkaterület törlése sikertelen: a munkaterület nem található. WorkspaceId: {WorkspaceId}", workspaceId);
                return false;
            }

            workspace.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogWarning("Munkaterület lomtárba helyezve (soft delete)! WorkspaceId: {WorkspaceId}, OwnerId: {OwnerId}", workspaceId, workspace.OwnerId);

            return true;
        }

        public async Task<WorkspaceStorageUsageResponseDto?> GetWorkspaceStorageAsync(Guid workspaceId)
        {
            var workspace = await _context.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == workspaceId && w.DeletedAt == null);

            if (workspace == null)
            {
                _logger.LogDebug("Tárhely-használat lekérdezése: nem található munkaterület. WorkspaceId: {WorkspaceId}", workspaceId);
                return null;
            }

            var usedBytes = await _context.StorageNodes
                .Where(n => n.WorkspaceId == workspaceId && n.DeletedAt == null && n.Type == 2)
                .SumAsync(n => (long)(n.SizeBytes ?? 0));

            return new WorkspaceStorageUsageResponseDto(workspace.Id, workspace.Name, (ulong)usedBytes);
        }

        public async Task<WorkspaceDataChangeResponseDto?> UpdateWorkspaceAsync(Guid workspaceId, WorkspaceDataChangeDto dto)
        {
            var workspace = await _context.Workspaces.FindAsync(workspaceId);
            if (workspace == null)
            {
                _logger.LogWarning("Munkaterület adatainak módosítása sikertelen: nem található. WorkspaceId: {WorkspaceId}", workspaceId);
                return null;
            }

            workspace.Name = dto.workspaceName;
            workspace.Description = dto.description;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Munkaterület adatai frissítve. WorkspaceId: {WorkspaceId}", workspace.Id);

            return new WorkspaceDataChangeResponseDto(workspace.Id, workspace.Name, workspace.Description);
        }

        public async Task<WorkspaceOwnerChangeResponseDto?> ChangeWorkspaceOwnerAsync(Guid workspaceId, WorkspaceOwnerChangeDto dto)
        {
            var workspace = await _context.Workspaces.FindAsync(workspaceId);
            if (workspace == null)
            {
                _logger.LogWarning("Tulajdonosváltás sikertelen: a munkaterület nem található. WorkspaceId: {WorkspaceId}", workspaceId);
                return null;
            }

            var newOwner = await _context.AppUsers.FindAsync(dto.newOwnerId);
            if (newOwner == null)
            {
                _logger.LogWarning("Tulajdonosváltás sikertelen: az új tulajdonos nem található. WorkspaceId: {WorkspaceId}, NewOwnerId: {NewOwnerId}",
                    workspaceId, dto.newOwnerId);
                throw new KeyNotFoundException("A megadott új tulajdonos nem található!");
            }

            var oldOwnerId = workspace.OwnerId;
            workspace.OwnerId = dto.newOwnerId;
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Munkaterület tulajdonosa megváltoztatva! WorkspaceId: {WorkspaceId}, RégiTulajdonos: {OldOwnerId}, ÚjTulajdonos: {NewOwnerId}",
                workspaceId, oldOwnerId, dto.newOwnerId);

            var membership = await _context.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == dto.newOwnerId);

            return new WorkspaceOwnerChangeResponseDto(
                workspace.Id, workspace.Name, newOwner.Id, newOwner.Name, membership?.RoleId ?? Guid.Empty
            );
        }

        public async Task<WorkspaceMemberRoleChangeResponseDto?> UpdateMemberRoleAsync(Guid workspaceId, Guid userId, WorkspaceMemberRoleChangeDto dto)
        {
            var membership = await _context.WorkspaceMembers
                .Include(m => m.User)
                .Include(m => m.Workspace)
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

            if (membership == null)
            {
                _logger.LogWarning("Munkaterület-tag szerepkörének módosítása sikertelen: a tagság nem található. WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    workspaceId, userId);
                return null;
            }

            var oldRoleId = membership.RoleId;
            membership.RoleId = dto.roleId;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Munkaterület-tag szerepköre módosítva. WorkspaceId: {WorkspaceId}, UserId: {UserId}, RégiRoleId: {OldRoleId}, ÚjRoleId: {NewRoleId}",
                workspaceId, userId, oldRoleId, dto.roleId);

            return new WorkspaceMemberRoleChangeResponseDto(
                membership.WorkspaceId, membership.Workspace.Name, membership.UserId, membership.User.Name, membership.RoleId
            );
        }

        public async Task<bool> RemoveMemberAsync(Guid workspaceId, Guid userId)
        {
            var membership = await _context.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

            if (membership == null)
            {
                _logger.LogWarning("Tag eltávolítása a munkaterületről sikertelen: a tagság nem található. WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    workspaceId, userId);
                return false;
            }

            _context.WorkspaceMembers.Remove(membership);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tag eltávolítva a munkaterületről. WorkspaceId: {WorkspaceId}, UserId: {UserId}", workspaceId, userId);

            return true;
        }

        public async Task<ReserveTaskResultDto> ReserveTaskAsync(ReserveTaskDto dto)
        {
            // 1. Aktív felhasználó ellenőrzése
            var user = await _context.AppUsers
    .FirstOrDefaultAsync(u => u.Id == dto.assigneeId && u.DeletedAt == null && u.IsActive == true);

            if (user == null)
            {
                _logger.LogWarning("Feladat lefoglalása sikertelen: nem létező felhasználó. TaskId: {TaskId}, UserId: {UserId}", dto.id, dto.assigneeId);
                throw new KeyNotFoundException("Nem található a felhasználó!");
            }

            // 2. Feladat lekérdezése úgy, hogy biztosan az adott projekthez tartozzon
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == dto.id && t.ProjectId == dto.projectId);

            if (task == null)
            {
                _logger.LogWarning("Feladat lefoglalása sikertelen: a feladat nem található a projektben. TaskId: {TaskId}, ProjectId: {ProjectId}, UserId: {UserId}",
                    dto.id, dto.projectId, user.Id);
                throw new KeyNotFoundException("Nem található a feladat a megadott projektben!");
            }

            // 3. Már foglalt-e?
            if (task.AssigneeUserId != null)
            {
                _logger.LogWarning("Feladat lefoglalása elutasítva: a feladat már foglalt. TaskId: {TaskId}, JelenlegiFelelős: {CurrentAssigneeId}, Kérelmező: {UserId}",
                    task.Id, task.AssigneeUserId, user.Id);
                throw new InvalidOperationException("A feladat már hozzá van rendelve valakihez!");
            }

            // 4. Projekttagság és meghirdetett szerepkör ellenőrzése
            var isEligible = await _context.ProjectMembers.AnyAsync(pm =>
                pm.ProjectId == dto.projectId &&
                pm.UserId == user.Id &&
                (pm.RoleId == task.AssigneeRoleId));

            if (!isEligible)
            {
                _logger.LogWarning("Feladat lefoglalása elutasítva: hiányzó projekttagság vagy szerepkör. TaskId: {TaskId}, ProjectId: {ProjectId}, UserId: {UserId}, SzükségesRoleId: {RequiredRoleId}",
                    task.Id, dto.projectId, user.Id, task.AssigneeRoleId);
                throw new UnauthorizedAccessException("A felhasználó nem tagja a projektnek, vagy nincs meg a feladathoz szükséges szerepköre!");
            }

            // 5. Módosítás és MENTÉS az adatbázisba
            task.AssigneeUserId = user.Id;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Feladat sikeresen lefoglalva. TaskId: {TaskId}, ProjectId: {ProjectId}, AssigneeUserId: {UserId}",
                task.Id, dto.projectId, user.Id);

            return new ReserveTaskResultDto(task.UpdatedAt, user.Id);
        }

        public async Task<bool> UnReserveTaskAsync(ReserveTaskDto dto)
        {
            // 1. Aktív felhasználó ellenőrzése
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == dto.assigneeId && u.DeletedAt == null);

            if (user == null)
            {
                _logger.LogWarning("Feladat lemondása sikertelen: nem létező felhasználó. TaskId: {TaskId}, UserId: {UserId}", dto.id, dto.assigneeId);
                throw new KeyNotFoundException("Nem található a felhasználó!");
            }

            // 2. Feladat lekérdezése az adott projektben
            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == dto.id && t.ProjectId == dto.projectId);

            if (task == null)
            {
                _logger.LogWarning("Feladat lemondása sikertelen: a feladat nem található a projektben. TaskId: {TaskId}, ProjectId: {ProjectId}, UserId: {UserId}",
                    dto.id, dto.projectId, user.Id);
                throw new KeyNotFoundException("Nem található a feladat a megadott projektben!");
            }

            // 3. Ellenőrizzük, hogy foglalt-e és valóban ő foglalta-e le
            if (task.AssigneeUserId == null)
            {
                _logger.LogWarning("Feladat lemondása elutasítva: a feladat nincs lefoglalva. TaskId: {TaskId}, UserId: {UserId}", task.Id, user.Id);
                throw new InvalidOperationException("A feladat még nincs hozzárendelve senkihez!");
            }

            if (task.AssigneeUserId != user.Id)
            {
                _logger.LogWarning("Jogosulatlan feladat-lemondási kísérlet: más felhasználóhoz rendelt feladat. TaskId: {TaskId}, ValódiFelelős: {ActualAssigneeId}, Kérelmező: {UserId}",
                    task.Id, task.AssigneeUserId, user.Id);
                throw new UnauthorizedAccessException("Csak a saját magad által lefoglalt feladatot mondhatod le!");
            }

            // 4. Projekttagság és meghirdetett szerepkör ellenőrzése
            var isEligible = await _context.ProjectMembers.AnyAsync(pm =>
                pm.ProjectId == dto.projectId &&
                pm.UserId == user.Id &&
                (task.AssigneeRoleId == null || pm.RoleId == task.AssigneeRoleId));

            if (!isEligible)
            {
                _logger.LogWarning("Feladat lemondása elutasítva: hiányzó projekttagság vagy szerepkör. TaskId: {TaskId}, ProjectId: {ProjectId}, UserId: {UserId}",
                    task.Id, dto.projectId, user.Id);
                throw new UnauthorizedAccessException("A felhasználó nem tagja a projektnek, vagy nincs meg a feladathoz szükséges szerepköre!");
            }

            // 5. Módosítás és mentés az adatbázisba
            task.AssigneeUserId = null;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Feladat lefoglalása sikeresen lemondva. TaskId: {TaskId}, ProjectId: {ProjectId}, UserId: {UserId}",
                task.Id, dto.projectId, user.Id);

            return true;
        }
    }
}