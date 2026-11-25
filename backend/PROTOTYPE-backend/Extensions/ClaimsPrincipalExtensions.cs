using System.Security.Claims;

namespace PROTOTYPE_backend.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsSystemAdmin(this ClaimsPrincipal user)
        {
            var role = user.FindFirstValue(ClaimTypes.Role);
            return role == "2" || role == "SystemAdmin";
        }

        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Érvénytelen vagy hiányzó felhasználói munkamenet.");
            }

            return userId;
        }
    }
}