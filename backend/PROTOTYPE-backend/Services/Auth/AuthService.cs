using PROTOTYPE_backend.Data.Repositories;
using PROTOTYPE_backend.DTOs.Auth;
using PROTOTYPE_backend.Models;
using BCrypt.Net;

namespace PROTOTYPE_backend.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IUnitOfWork unitOfWork) 
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto) 
        {
            var userExistsByEmail = await _unitOfWork.Repository<AppUser>()
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            var userExistsByName = await _unitOfWork.Repository<AppUser>()
                .FirstOrDefaultAsync(u => u.Name == dto.Username);

            if (userExistsByEmail != null)
            {
                throw new Exception("Az email cím már foglalt!");
            }

            if (userExistsByName != null) 
            {
                throw new Exception("A felhasználó név már foglalt!");
            }

            var newUser = new AppUser()
            {
                Name = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(dto.Password, 13),
                CreatedAt = DateTime.UtcNow,
            };

            await _unitOfWork.Repository<AppUser>().AddAsync(newUser);
            await _unitOfWork.CompleteAsync();

            return new UserDto
            {
                Id = newUser.Id,
                Username = newUser.Name,
                Email = newUser.Email,
                CreatedAt = newUser.CreatedAt,
            };
        }
    }
}
