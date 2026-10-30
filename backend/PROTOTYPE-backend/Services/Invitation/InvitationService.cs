using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Invitations;
using PROTOTYPE_backend.Models;
using System.Security.Cryptography;

namespace PROTOTYPE_backend.Services.Invitations
{
    public class InvitationService : IInvitationService
    {
        private readonly AppDbContext _context;

        public InvitationService(AppDbContext context)
        {
            _context = context;
        }

        // 1. MEGHÍVÓ LÉTREHOZÁSA
        public async Task<InvitationResponseDto> CreateInvitationAsync(Guid currentUserId, CreateInvitationDto dto)
        {
            // Megkeressük, hogy a meghívott emailhez tartozik-e már létező felhasználó
            var invitedUser = await _context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == dto.InvitedEmail.ToLower().Trim() && u.DeletedAt == null);

            var invitation = new AppInvitation
            {
                Id = Guid.NewGuid(),
                Type = dto.Type,
                TargetId = dto.TargetId,
                InvitedEmail = dto.InvitedEmail.ToLower().Trim(),
                InvitedUserId = invitedUser?.Id,
                RoleId = dto.RoleId,
                InvitedBy = currentUserId,
                Status = 1, // 1 = pending
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), // Biztonságos egyedi token
                ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 napos lejárati idő
                CreatedAt = DateTime.UtcNow
            };

            _context.AppInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            return MapToResponseDto(invitation);
        }

        // 2. BEJELENTKEZETT FELHASZNÁLÓ MEGHÍVÓINAK LEKÉRÉSE
        public async Task<IEnumerable<InvitationResponseDto>> GetMyInvitationsAsync(Guid currentUserId)
        {
            var user = await _context.AppUsers.FindAsync(currentUserId);
            if (user == null) return Enumerable.Empty<InvitationResponseDto>();

            var now = DateTime.UtcNow;

            // Frissítjük a lejárt meghívókat
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

            if (invitation == null) return null;

            string targetName = "Ismeretlen";

            if (invitation.Type == 1) // Workspace
            {
                var ws = await _context.Workspaces.FindAsync(invitation.TargetId);
                if (ws != null) targetName = ws.Name;
            }
            else if (invitation.Type == 2) // Project
            {
                var proj = await _context.Projects.FindAsync(invitation.TargetId);
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

            if (invitation == null || invitation.Status != 1 || invitation.ExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            var user = await _context.AppUsers.FindAsync(currentUserId);
            if (user == null) return false;

            // Státusz frissítése
            invitation.Status = 2; // 2 = accepted
            invitation.InvitedUserId = currentUserId;
            invitation.RespondedAt = DateTime.UtcNow;

            // Hozzáadás a megfelelő tagsághoz
            if (invitation.Type == 1) // Workspace meghívó
            {
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
                var project = await _context.Projects.FindAsync(invitation.TargetId);
                if (project != null && invitation.RoleId.HasValue)
                {
                    var exists = await _context.ProjectMembers
                        .AnyAsync(pm => pm.ProjectId == invitation.TargetId && pm.UserId == currentUserId && pm.RoleId == invitation.RoleId.Value);

                    if (!exists)
                    {
                        // Megnézzük, tagja-e a felettes workspace-nek
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
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // 6. MEGHÍVÓ ELUTASÍTÁSA
        public async Task<bool> RefuseInvitationAsync(Guid currentUserId, string token)
        {
            var invitation = await _context.AppInvitations.FirstOrDefaultAsync(i => i.Token == token);

            if (invitation == null || invitation.Status != 1) return false;

            invitation.Status = 3; // 3 = refused
            invitation.InvitedUserId = currentUserId;
            invitation.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // 7. MEGHÍVÓ TÖRLESE / VISSZAVONÁSA
        public async Task<bool> CancelInvitationAsync(Guid currentUserId, Guid invitationId)
        {
            var invitation = await _context.AppInvitations.FindAsync(invitationId);
            if (invitation == null) return false;

            _context.AppInvitations.Remove(invitation);
            await _context.SaveChangesAsync();
            return true;
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