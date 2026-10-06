// KM

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Token
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration) 
        {
            _configuration = configuration;
        }

        public string GenerateGlobalToken(AppUser user)
        { 
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.GlobalRole.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            return BuildJwt(claims);
        }

        public string GenerateWorkspaceToken(
            AppUser user,
            Guid workspaceId,
            Dictionary<string, List<string>> WorkspaceRolesNPermissions,
            Dictionary<string, List<string>> ProjectRolesNPermissions
            ) 
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.GlobalRole.ToString()),
                new("active_workspace_id", workspaceId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            string wRolesNPermissions = JsonSerializer.Serialize(WorkspaceRolesNPermissions);
            string pRolesNPermissions = JsonSerializer.Serialize(ProjectRolesNPermissions);

            claims.Add(new Claim("active_workspace_roles_permissions", wRolesNPermissions));
            claims.Add(new Claim("project_roles_permissions", pRolesNPermissions));

            return BuildJwt(claims);
        }

        private string BuildJwt(IEnumerable<Claim> claims) 
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"]
                ?? throw new InvalidOperationException("A JWT Secret nincsen konfigurálva.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiryInMinutes = double.Parse(jwtSettings["ExpiryInMinutes"] ?? "10");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiryInMinutes),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken() 
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
