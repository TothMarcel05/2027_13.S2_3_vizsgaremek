// KM

using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Token
{
    public interface ITokenService
    {
        string GenerateGlobalToken(AppUser user);
        string GenerateWorkspaceToken(
            AppUser user,
            Guid workspaceId,
            Dictionary<string, List<string>> AllRolesNPermissions
        );
        string GenerateRefreshToken();
        string GenerateTwoFactorToken(AppUser user);
        Guid? ValidateTwoFactorToken(string token);
    }
}
