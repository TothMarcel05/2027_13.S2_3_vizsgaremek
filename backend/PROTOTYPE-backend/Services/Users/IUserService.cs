using PROTOTYPE_backend.DTOs.Users;

namespace PROTOTYPE_backend.Services.Users
{
    public interface IUserService
    {
        Task<UserResponseDto> UserModifyAsync(Guid userId, UserModifyRequestDto dto);
        Task<UserResponseDto> UserDeleteAsync(Guid userId);

    }
}
