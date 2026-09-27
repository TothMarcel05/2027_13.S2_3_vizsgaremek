using PROTOTYPE_backend.DTOs;
using PROTOTYPE_backend.DTOs.Auth;

namespace PROTOTYPE_backend.Services.Auth
{
    public interface IAuthService
    {
        Task<UserDto> CreateUserAsync(CreateUserDto dto);
    }
}
