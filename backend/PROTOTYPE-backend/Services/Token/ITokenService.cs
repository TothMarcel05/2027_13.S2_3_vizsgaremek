// KM

using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Token
{
    public interface ITokenService
    {
        string GenerateGlobalToken(AppUser user);
        string GenerateWorkspaceToken(
            AppUser user,
            string workspaceId,
            string workspaceRole,
            IEnumerable<(string ProjectId, string RoleName)> projectRoles
        );
        string GenerateRefreshToken();
    }
}
