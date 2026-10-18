using PROTOTYPE_backend.DTOs;
using PROTOTYPE_backend.DTOs.Auth;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Auth
{
    public interface IAuthService
    {
        Task<UserDto> CreateUserAsync(RegisterDto dto);
        Task<LoginResultDto> LoginAsync(LoginDto dto);
        Task LogoutAsync(Guid userId, string refreshToken);
        Task<AuthResponseDto> RefreshAsync(string refreshToken);
        Task<AuthResponseDto> IssueTokensAsync(AppUser user);
        Task<AuthResponseDto> VerifyTwoFactorAsync(TwoFactorLoginDto dto);
        Task<TwoFactorSetupDto> BeginTwoFactorSetupAsync(Guid userId);
        Task<List<string>> EnableTwoFactorAsync(Guid userId, string code);
    }
}
