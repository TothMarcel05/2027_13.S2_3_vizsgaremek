using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PROTOTYPE_backend.Models;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PROTOTYPE_backend.Services.Token
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;
        private static readonly JwtSecurityTokenHandler TokenHandler = new();

        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateGlobalToken(AppUser user)
        {
            var jti = Guid.NewGuid().ToString();
            List<Claim> claims = [
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.GlobalRole.ToString()),
                new(JwtRegisteredClaimNames.Jti, jti)
            ];

            _logger.LogInformation("Globális JWT token generálva. UserId: {UserId}, Jti: {Jti}", user.Id, jti);

            return BuildJwt(claims);
        }

        public string GenerateWorkspaceToken(
            AppUser user,
            Guid workspaceId,
            Dictionary<string, List<string>> allRolesNPermissions)
        {
            var jti = Guid.NewGuid().ToString();
            List<Claim> claims = [
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.GlobalRole.ToString()),
                new("active_workspace_id", workspaceId.ToString()),
                new(JwtRegisteredClaimNames.Jti, jti)
            ];

            string serializedPermissions = JsonSerializer.Serialize(allRolesNPermissions);
            claims.Add(new Claim("all_roles_permissions", serializedPermissions));

            _logger.LogInformation(
                "Workspace JWT token generálva. UserId: {UserId}, WorkspaceId: {WorkspaceId}, Jti: {Jti}",
                user.Id, workspaceId, jti);

            return BuildJwt(claims);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public string GenerateTwoFactorToken(AppUser user)
        {
            _logger.LogDebug("2FA ideiglenes token generálva. UserId: {UserId}", user.Id);
            return BuildJwt([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())],
                audienceSuffix: "-2fa", expiryMinutes: 5);
        }

        public Guid? ValidateTwoFactorToken(string token)
        {
            var jwt = _configuration.GetSection("JwtSettings");
            try
            {
                var principal = TokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwt["Audience"] + "-2fa",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                }, out _);

                return Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Lejárt 2FA token érvényesítési kísérlet.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Érvénytelen 2FA token érvényesítési kísérlet.");
                return null;
            }
        }

        private string BuildJwt(IEnumerable<Claim> claims, string? audienceSuffix = null, double? expiryMinutes = null)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"];

            if (string.IsNullOrEmpty(secretKey))
            {
                _logger.LogCritical("A JWT Secret nincs konfigurálva az appsettings-ben!");
                throw new InvalidOperationException("A JWT Secret nincsen konfigurálva.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var expiry = expiryMinutes
                ?? double.Parse(jwtSettings["ExpiryInMinutes"] ?? "10", CultureInfo.InvariantCulture);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiry),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"] + audienceSuffix,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            return TokenHandler.WriteToken(TokenHandler.CreateToken(tokenDescriptor));
        }
    }
}