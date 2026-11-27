using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Invitations;
using PROTOTYPE_backend.Models;
using System.Security.Cryptography;

namespace PROTOTYPE_backend.Services.Invitations
{
    public class InvitationService : IInvitationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<InvitationService> _logger;

        public InvitationService(AppDbContext context, ILogger<InvitationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 1. MEGHÍVÓ LÉTREHOZÁSA
        public async Task<InvitationResponseDto> CreateInvitationAsync(Guid currentUserId, CreateInvitationDto dto)
        {
            var normalizedEmail = dto.InvitedEmail.ToLower().Trim();

            var invitedUser = await _context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.DeletedAt == null && u.IsActive == true);

            var invitation = new AppInvitation
            {
                Id = Guid.NewGuid(),
                Type = dto.Type,
                TargetId = dto.TargetId,
                InvitedEmail = normalizedEmail,
                InvitedUserId = invitedUser?.Id,
                RoleId = dto.RoleId,
                InvitedBy = currentUserId,
                Status = 1, // 1 = pending
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _context.AppInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Új meghívó létrehozva. InvitationId: {InvitationId}, Type: {InvitationType}, TargetId: {TargetId}, RoleId: {RoleId}, InvitedBy: {UserId}, Meghívott: {MaskedEmail}, LétezőFelhasználó: {IsExistingUser}",
                invitation.Id, invitation.Type, invitation.TargetId, invitation.RoleId, currentUserId, MaskEmail(normalizedEmail), invitedUser != null);

            return MapToResponseDto(invitation);
        }

        // 2. BEJELENTKEZETT FELHASZNÁLÓ MEGHÍVÓINAK LEKÉRÉSE
        public async Task<IEnumerable<InvitationResponseDto>> GetMyInvitationsAsync(Guid currentUserId)
        {
            var user = await _context.AppUsers.FindAsync(currentUserId);
            if (user == null)
            {
                _logger.LogWarning("Meghívók lekérdezése sikertelen: a felhasználó nem található. UserId: {UserId}", currentUserId);
                return Enumerable.Empty<InvitationResponseDto>();
            }

            var now = DateTime.UtcNow;

            var expiredInvitations = await _context.AppInvitations
                .Where(i => (i.InvitedUserId == currentUserId || i.InvitedEmail == user.Email) && i.Status == 1 && i.ExpiresAt < now)
                .ToListAsync();

            if (expiredInvitations.Any())
            {
                foreach (var inv in expiredInvitations)
                {
                    inv.Status = 4; // 4 = expired
                }
                await _context.SaveChangesAsync();

                _logger.LogDebug(
                    "{ExpiredCount} lejárt meghívó státusza 'expired' (4) értékre állítva. UserId: {UserId}",
                    expiredInvitations.Count, currentUserId);
            }

            return await _context.AppInvitations
                .AsNoTracking()
                .Where(i => i.InvitedUserId == currentUserId || i.InvitedEmail == user.Email)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => MapToResponseDto(i))
                .ToListAsync();
        }

        // 3. TARGET (WORKSPACE / PROJECT) FÜGGŐBEN LÉVŐ MEGHÍVÓI
        public async Task<IEnumerable<InvitationResponseDto>> GetPendingInvitationsByTargetAsync(Guid targetId, sbyte type)
        {
            return await _context.AppInvitations
                .AsNoTracking()
                .Where(i => i.TargetId == targetId && i.Type == type && i.Status == 1 && i.ExpiresAt > DateTime.UtcNow)
                .Select(i => MapToResponseDto(i))
                .ToListAsync();
        }

        // 4. MEGHÍVÓ ADATAI TOKEN ALAPJÁN (PUBLIKUS/ELŐNÉZET)
        public async Task<InvitationDetailDto?> GetInvitationByTokenAsync(string token)
        {
            var invitation = await _context.AppInvitations
                .AsNoTracking()
                .Include(i => i.InvitedByNavigation)
                .FirstOrDefaultAsync(i => i.Token == token);

            if (invitation == null)
            {
                _logger.LogDebug("Meghívó előnézet lekérdezése sikertelen: érvénytelen token.");
                return null;
            }

            string targetName = "Ismeretlen";

            if (invitation.Type == 1) // Workspace
            {
                var ws = await _context.Workspaces.AsNoTracking().FirstOrDefaultAsync(w => w.Id == invitation.TargetId && w.DeletedAt == null);
                if (ws != null) targetName = ws.Name;
            }
            else if (invitation.Type == 2) // Project
            {
                var proj = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == invitation.TargetId && p.DeletedAt == null);
                if (proj != null) targetName = proj.Name;
            }

            return new InvitationDetailDto(
                invitation.Id,
                invitation.Type,
                invitation.TargetId,
                targetName,
                invitation.InvitedEmail,
                invitation.InvitedByNavigation.Name,
                invitation.ExpiresAt,
                invitation.Status
            );
        }

        // 5. MEGHÍVÓ ELFOGADÁSA (AUTOMATIKUS TAGSÁG LÉTREHOZÁSÁVAL)
        public async Task<bool> AcceptInvitationAsync(Guid currentUserId, string token)
        {
            var invitation = await _context.AppInvitations.FirstOrDefaultAsync(i => i.Token == token);

            if (invitation == null)
            {
                _logger.LogWarning("Meghívó elfogadása sikertelen: nem létező token. UserId: {UserId}", currentUserId);
                return false;
            }

            if (invitation.Status != 1 || invitation.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Meghívó elfogadása elutasítva: lejárt vagy már nem függőben lévő meghívó. InvitationId: {InvitationId}, Status: {Status}, ExpiresAt: {ExpiresAt}, UserId: {UserId}",
                    invitation.Id, invitation.Status, invitation.ExpiresAt, currentUserId);
                return false;
            }

            var user = await _context.AppUsers.FindAsync(currentUserId);
            if (user == null)
            {
                _logger.LogWarning("Meghívó elfogadása sikertelen: a felhasználó nem található. InvitationId: {InvitationId}, UserId: {UserId}",
                    invitation.Id, currentUserId);
                return false;
            }

            invitation.Status = 2; // 2 = accepted
            invitation.InvitedUserId = currentUserId;
            invitation.RespondedAt = DateTime.UtcNow;

            if (invitation.Type == 1) // Workspace meghívó
            {
                var workspaceExists = await _context.Workspaces.AnyAsync(w => w.Id == invitation.TargetId && w.DeletedAt == null);
                if (!workspaceExists) return false;

                var exists = await _context.WorkspaceMembers
                    .AnyAsync(wm => wm.WorkspaceId == invitation.TargetId && wm.UserId == currentUserId);

                if (!exists && invitation.RoleId.HasValue)
                {
                    _context.WorkspaceMembers.Add(new WorkspaceMember
                    {
                        WorkspaceId = invitation.TargetId,
                        UserId = currentUserId,
                        RoleId = invitation.RoleId.Value,
                        JoinedAt = DateTime.UtcNow
                    });
                }
            }
            else if (invitation.Type == 2) // Project meghívó
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == invitation.TargetId && p.DeletedAt == null);
                if (project == null)
                {
                    _logger.LogError(
                        "Adatinkonzisztencia: projekt meghívó elfogadásakor a célprojekt nem létezik vagy törölt! InvitationId: {InvitationId}, TargetProjectId: {TargetId}, UserId: {UserId}",
                        invitation.Id, invitation.TargetId, currentUserId);
                    return false;
                }

                if (project != null && invitation.RoleId.HasValue)
                {
                    var exists = await _context.ProjectMembers
                        .AnyAsync(pm => pm.ProjectId == invitation.TargetId && pm.UserId == currentUserId && pm.RoleId == invitation.RoleId.Value);

                    if (!exists)
                    {
                        var isWsMember = await _context.WorkspaceMembers
                            .AnyAsync(wm => wm.WorkspaceId == project.WorkspaceId && wm.UserId == currentUserId);

                        _context.ProjectMembers.Add(new ProjectMember
                        {
                            ProjectId = project.Id,
                            WorkspaceId = project.WorkspaceId,
                            UserId = currentUserId,
                            RoleId = invitation.RoleId.Value,
                            IsWorkspaceMember = isWsMember,
                            AssignedAt = DateTime.UtcNow
                        });
                    }
                }
                else if (project == null)
                {
                    _logger.LogError(
                        "Adatinkonzisztencia: projekt meghívó elfogadásakor a célprojekt már nem létezik! InvitationId: {InvitationId}, TargetProjectId: {TargetId}, UserId: {UserId}",
                        invitation.Id, invitation.TargetId, currentUserId);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Meghívó elfogadva, tagság rögzítve. InvitationId: {InvitationId}, Type: {InvitationType}, TargetId: {TargetId}, RoleId: {RoleId}, UserId: {UserId}",
                invitation.Id, invitation.Type, invitation.TargetId, invitation.RoleId, currentUserId);

            return true;
        }

        // 6. MEGHÍVÓ ELUTASÍTÁSA
        public async Task<bool> RefuseInvitationAsync(Guid currentUserId, string token)
        {
            var invitation = await _context.AppInvitations.FirstOrDefaultAsync(i => i.Token == token);

            if (invitation == null || invitation.Status != 1)
            {
                _logger.LogWarning("Meghívó elutasítása sikertelen: érvénytelen vagy nem függőben lévő meghívó. InvitationId: {InvitationId}, UserId: {UserId}",
                    invitation?.Id, currentUserId);
                return false;
            }

            invitation.Status = 3; // 3 = refused
            invitation.InvitedUserId = currentUserId;
            invitation.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Meghívó elutasítva a felhasználó által. InvitationId: {InvitationId}, TargetId: {TargetId}, UserId: {UserId}",
                invitation.Id, invitation.TargetId, currentUserId);

            return true;
        }

        // 7. MEGHÍVÓ TÖRLÉSE / VISSZAVONÁSA
        public async Task<bool> CancelInvitationAsync(Guid currentUserId, Guid invitationId)
        {
            var invitation = await _context.AppInvitations.FindAsync(invitationId);
            if (invitation == null)
            {
                _logger.LogWarning("Meghívó visszavonása sikertelen: nem található. InvitationId: {InvitationId}, InitiatorUserId: {UserId}",
                    invitationId, currentUserId);
                return false;
            }

            var targetId = invitation.TargetId;
            var type = invitation.Type;

            _context.AppInvitations.Remove(invitation);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Meghívó visszavonva (törölve). InvitationId: {InvitationId}, Type: {InvitationType}, TargetId: {TargetId}, InitiatorUserId: {UserId}",
                invitationId, type, targetId, currentUserId);

            return true;
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return "***";

            var parts = email.Split('@');
            var local = parts[0];
            var domain = parts[1];

            var visiblePrefix = local.Length <= 2 ? local[..1] : local[..2];
            return $"{visiblePrefix}***@{domain}";
        }

        private static InvitationResponseDto MapToResponseDto(AppInvitation i)
        {
            return new InvitationResponseDto(
                i.Id,
                i.Type,
                i.TargetId,
                i.InvitedEmail,
                i.InvitedUserId,
                i.RoleId,
                i.InvitedBy,
                i.Status,
                i.Token,
                i.ExpiresAt,
                i.CreatedAt ?? DateTime.UtcNow,
                i.RespondedAt
            );
        }
    }
}