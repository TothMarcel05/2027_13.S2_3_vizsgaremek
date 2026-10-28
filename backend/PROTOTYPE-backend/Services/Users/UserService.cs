using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Users;

namespace PROTOTYPE_backend.Services.Users
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        // 1. SAJÁT PROFIL LEKÉRDEZÉSE
        public async Task<UserResponseDto?> GetUserProfileAsync(Guid userId)
        {
            var user = await _context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

            if (user == null) return null;

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
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

            if (user == null) return null;

            user.Name = dto.Name;
            user.AvatarUrl = dto.AvatarURL;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

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
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

            if (user == null) return false;

            // Jelenlegi jelszó ellenőrzése
            bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
            if (!isCurrentPasswordValid) return false;

            // Új jelszó hashelése és mentése
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // 4. SOFT-DELETE (FIÓK TÖRLESE)
        public async Task<UserDeleteResponseDto?> DeleteUserAccountAsync(Guid userId)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

            if (user == null) return null;

            var deletedTime = DateTime.UtcNow;
            user.DeletedAt = deletedTime;

            await _context.SaveChangesAsync();

            return new UserDeleteResponseDto(
                user.Id,
                deletedTime
            );
        }

        // 5. CSAPATTAG EGYEDI NYILVÁNOS PROFILJA
        public async Task<UserPublicDto?> GetPublicProfileByIdAsync(Guid targetUserId)
        {
            return await _context.AppUsers
                .AsNoTracking()
                .Where(u => u.Id == targetUserId && u.DeletedAt == null)
                .Select(u => new UserPublicDto(
                    u.Id,
                    u.Name,
                    u.Email,
                    u.AvatarUrl
                ))
                .FirstOrDefaultAsync();
        }

        // 6. CSAPATTAGOK KERESÉSE (PROJEKT Hozzárendeléshez / Név vagy Email alapján)
        public async Task<IEnumerable<UserPublicDto>> SearchUsersAsync(string searchTerm)
        {
            var query = searchTerm.ToLower().Trim();

            return await _context.AppUsers
                .AsNoTracking()
                .Where(u => u.DeletedAt == null &&
                           (u.Name.ToLower().Contains(query) || u.Email.ToLower().Contains(query)))
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