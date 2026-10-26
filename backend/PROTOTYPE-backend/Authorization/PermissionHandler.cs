using Microsoft.AspNetCore.Authorization;
using PROTOTYPE_backend.Authorization;
using System.Security.Claims;
using System.Text.Json;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // 1. Rendszeradminisztrátor ellenőrzése (GlobalRole = "2" esetén minden átmegy)
        var globalRoleClaim = context.User.FindFirstValue(ClaimTypes.Role);
        if (globalRoleClaim == "2" || globalRoleClaim == "SystemAdmin")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. Kinyerjük a JWT-ben tárolott all_roles_permissions JSON-t
        var permissionsJson = context.User.FindFirstValue("all_roles_permissions");
        if (string.IsNullOrEmpty(permissionsJson))
        {
            return Task.CompletedTask; // Nincs claim, megtagadjuk a hozzáférést (403)
        }

        try
        {
            // Deszerializáljuk a Dictionary<string, List<string>> struktúrát
            var rolesAndPermissions = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(permissionsJson);

            if (rolesAndPermissions != null)
            {
                // 3. Megnézzük, hogy a felhasználó bármelyik Role-ja tartalmazza-e a kért permission-t
                var hasPermission = rolesAndPermissions.Values
                    .SelectMany(permissionsList => permissionsList)
                    .Contains(requirement.Permission);

                if (hasPermission)
                {
                    context.Succeed(requirement);
                }
            }
        }
        catch
        {
            // Hibás JSON format esetén nem engedjük át
        }

        return Task.CompletedTask;
    }
}