using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Users;
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

        public async Task<UserResponseDto> UserModifyAsync(Guid userId, UserModifyRequestDto dto) 
        {

        }
    }
}
