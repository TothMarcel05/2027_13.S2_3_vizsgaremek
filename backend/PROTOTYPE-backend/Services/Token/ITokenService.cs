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
            Dictionary<string, List<string>> workspaceRolesNPermissions,
            Dictionary<string, List<string>> projectRolesNPermssions
        );
        string GenerateRefreshToken();
    }
}
