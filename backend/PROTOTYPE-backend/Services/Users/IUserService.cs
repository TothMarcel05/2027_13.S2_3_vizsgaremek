using PROTOTYPE_backend.DTOs.Users;

namespace PROTOTYPE_backend.Services.Users
{
    public interface IUserService
    {
        Task<UserResponseDto> UserInformationAsync(Guid userId);
        Task<UserModfiyResponseDto> UserModifyAsync(Guid userId, UserModifyRequestDto dto);
        Task<UserDeleteResponseDto> UserDeleteAsync(Guid userId);

    }
}
