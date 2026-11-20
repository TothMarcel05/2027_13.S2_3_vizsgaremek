using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Users;

namespace PROTOTYPE_backend.Services.Users
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(AppDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 1. SAJÁT PROFIL LEKÉRDEZÉSE
        public async Task<UserResponseDto?> GetUserProfileAsync(Guid userId)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive == true);

            if (user == null)
            {
                _logger.LogWarning("Saját profil lekérdezése sikertelen: a felhasználó nem található vagy törölt. UserId: {UserId}", userId);
                return null;
            }

            return new UserResponseDto(
                user.Id,
                user.Name,
                user.Email,
                user.AvatarUrl,
                user.CreatedAt,
                user.UpdatedAt,
                user.GlobalRole,
                user.Is2faEnabled,
                user.TwoFactorType
            );
        }

        // 2. PROFIL MÓDOSÍTÁSA
        public async Task<UserModfiyResponseDto?> UpdateUserProfileAsync(Guid userId, UserModifyRequestDto dto)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive == true);

            if (user == null)
            {
                _logger.LogWarning("Profil módosítása sikertelen: a felhasználó nem található vagy törölt. UserId: {UserId}", userId);
                return null;
            }

            user.Name = dto.Name;
            user.AvatarUrl = dto.AvatarURL;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Felhasználói profil módosítva. UserId: {UserId}", user.Id);

            return new UserModfiyResponseDto(
                user.Id,
                user.Name,
                user.Email,
                user.AvatarUrl,
                user.UpdatedAt.Value
            );
        }

        // 3. JELSZÓ VÁLTOZTATÁS
        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto dto)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive == true);

            if (user == null)
            {
                _logger.LogWarning("Jelszóváltoztatás sikertelen: a felhasználó nem található, törölt vagy felfüggesztett. UserId: {UserId}", userId);
                return false;
            }

            bool isCurrentPasswordValid = BCrypt.Net.BCrypt.EnhancedVerify(dto.CurrentPassword, user.PasswordHash);
            if (!isCurrentPasswordValid)
            {
                _logger.LogWarning("Jelszóváltoztatás elutasítva: hibás jelenlegi jelszó. UserId: {UserId}", userId);
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(dto.NewPassword, 13);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Felhasználó jelszava sikeresen módosítva. UserId: {UserId}", userId);

            return true;
        }

        // 4. SOFT-DELETE (FIÓK TÖRLÉSE)
        public async Task<UserDeleteResponseDto?> DeleteUserAccountAsync(Guid userId)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive == true);

            if (user == null)
            {
                _logger.LogWarning("Fióktörlés sikertelen: a felhasználó nem található vagy már törölve van. UserId: {UserId}", userId);
                return null;
            }

            var deletedTime = DateTime.UtcNow;
            user.DeletedAt = deletedTime;

            await _context.SaveChangesAsync();

            _logger.LogWarning("Felhasználói fiók törölve (soft delete). UserId: {UserId}, DeletedAt: {DeletedAt}", user.Id, deletedTime);

            return new UserDeleteResponseDto(
                user.Id,
                deletedTime
            );
        }

        // 5. CSAPATTAG EGYEDI NYILVÁNOS PROFILJA
        public async Task<UserPublicDto?> GetPublicProfileByIdAsync(Guid targetUserId)
        {
            var profile = await _context.AppUsers
                .AsNoTracking()
                .Where(u => u.Id == targetUserId && u.DeletedAt == null && u.IsActive == true)
                .Select(u => new UserPublicDto(
                    u.Id,
                    u.Name,
                    u.Email,
                    u.AvatarUrl
                ))
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                _logger.LogDebug("Nyilvános profil lekérdezése: nem található felhasználó. TargetUserId: {TargetUserId}", targetUserId);
            }

            return profile;
        }

        // 6. CSAPATTAGOK KERESÉSE (PROJEKT Hozzárendeléshez / Név vagy Email alapján)
        public async Task<IEnumerable<UserPublicDto>> SearchUsersAsync(string searchTerm)
        {
            var query = searchTerm.ToLower().Trim();

            return await _context.AppUsers
                .AsNoTracking()
                .Where(u => u.DeletedAt == null 
                    && u.IsActive == true 
                    && (u.Name.ToLower().Contains(query) || u.Email.ToLower().Contains(query)))
                .Take(20) // Teljesítmény védelmében maximum 20 találat
                .Select(u => new UserPublicDto(
                    u.Id,
                    u.Name,
                    u.Email,
                    u.AvatarUrl
                ))
                .ToListAsync();
        }
    }
}