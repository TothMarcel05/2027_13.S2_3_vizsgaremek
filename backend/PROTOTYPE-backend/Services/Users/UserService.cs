using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Users;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.Services.Users;

namespace PROTOTYPE_backend.Services.User
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserResponseDto> UserInformationAsync(Guid userId) 
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) 
            {
                throw new Exception("A felhasználó nem található a rendszerben!");
            }

            var updatedat = user.UpdatedAt == null ? null : user.UpdatedAt;

            return new UserResponseDto
            (
                user.Id,
                user.Name,
                user.Email,
                updatedat,
                user.GlobalRole,
                user.Is2faEnabled,
                user.TwoFactorType
            );
        }

        public async Task<UserModfiyResponseDto> UserModifyAsync(Guid userId, UserModifyRequestDto dto)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new Exception("A felhasználó nem található a rendszerben!");
            }

            user.Name = dto.Name;
            user.AvatarUrl = dto.AvatarURL;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UserModfiyResponseDto
            (
                user.Id,
                user.Name,
                user.Email,
                (DateTime)user.UpdatedAt
            );
        }

        public async Task<UserDeleteResponseDto> UserDeleteAsync(Guid userId)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync (u => u.Id == userId);

            if (user == null) 
            {
                throw new Exception("A felhasználó nem található a rendszerben!");
            }

            user.DeletedAt = DateTime.UtcNow;

            _context.Remove(user);
            await _context.SaveChangesAsync();

            return new UserDeleteResponseDto
            (
                user.Id,
                (DateTime)user.DeletedAt
            );
        }
    }
}
