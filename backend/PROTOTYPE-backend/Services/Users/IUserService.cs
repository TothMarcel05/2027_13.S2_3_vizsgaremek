using PROTOTYPE_backend.DTOs.Users;

namespace PROTOTYPE_backend.Services.Users
{
    public interface IUserService
    {
        // Saját fiók kezelése
        Task<UserResponseDto?> GetUserProfileAsync(Guid userId);
        Task<UserModfiyResponseDto?> UpdateUserProfileAsync(Guid userId, UserModifyRequestDto dto);
        Task<UserDeleteResponseDto?> DeleteUserAccountAsync(Guid userId);

        // Biztonsági műveletek
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto dto);

        // Csapatmunka és keresés
        Task<UserPublicDto?> GetPublicProfileByIdAsync(Guid targetUserId);
        Task<IEnumerable<UserPublicDto>> SearchUsersAsync(string searchTerm);
    }
}