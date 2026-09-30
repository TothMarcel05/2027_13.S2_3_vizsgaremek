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
            Guid workspaceRole,
            Dictionary<Guid, List<string>> projectRoles
        );
        string GenerateRefreshToken();
    }
}
