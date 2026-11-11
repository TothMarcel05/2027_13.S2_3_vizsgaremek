
// KM

using BCrypt.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Auth;
using PROTOTYPE_backend.DTOs.Workspace;
using PROTOTYPE_backend.Models;
using PROTOTYPE_backend.Services.Email;
using PROTOTYPE_backend.Services.Token;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PROTOTYPE_backend.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _context;
        private readonly IDataProtector _protector;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthService(
            ITokenService tokenService,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context,
            IDataProtectionProvider protector,
            IConfiguration config,
            IEmailService emailService
            ) 
        {
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _protector = protector.CreateProtector("TwoFactorAuth");
            _config = config;
            _emailService = emailService;
            
        }

        private string GetClientIpAddress() 
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null) return String.Empty;

            var forwardedHeader = httpContext.Request.Headers["X-Forwarded-for"].FirstOrDefault();
            if (!String.IsNullOrEmpty(forwardedHeader))
                return forwardedHeader.Split(',')[0].Trim();

            return httpContext.Connection.RemoteIpAddress?.ToString() ?? String.Empty;
        }

        private string GetClientDeviceInfo() 
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return "Unknown";

            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            return string.IsNullOrEmpty(userAgent) ? "Unknown Device" : userAgent;
        }

        public async Task<UserDto> CreateUserAsync(RegisterDto dto) 
        {
            var userExistsByEmail = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            var userExistsByName = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Name == dto.Username);

            if (userExistsByEmail != null)
            {
                throw new Exception("Az email cím már foglalt!");
            }

            if (userExistsByName != null) 
            {
                throw new Exception("A felhasználó név már foglalt!");
            }

            var newUser = new AppUser()
            {
                Name = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(dto.Password, 13),
                CreatedAt = DateTime.UtcNow,
            };

            await _context.AppUsers.AddAsync(newUser);
            await _context.SaveChangesAsync();

            return new UserDto(newUser.Id, newUser.Name, newUser.Email, newUser.CreatedAt);
        }

        public async Task<LoginResultDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.DeletedAt == null);

            if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(loginDto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Érvénytelen e-mail cím vagy jelszó!");

            if (user.Is2faEnabled)
            {
                // if (user.TwoFactorType == 2) await SendEmailCodeAsync(user);   // lásd lent
                return new LoginResultDto(true, user.TwoFactorType, _tokenService.GenerateTwoFactorToken(user), null);
            }

            return new LoginResultDto(false, null, null, await IssueTokensAsync(user));
        }

        public async Task<AuthResponseDto> VerifyTwoFactorAsync(TwoFactorLoginDto dto)
        {
            var userId = _tokenService.ValidateTwoFactorToken(dto.TwoFactorToken)
                ?? throw new UnauthorizedAccessException("Lejárt vagy érvénytelen 2FA munkamenet!");

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.Is2faEnabled)
                ?? throw new UnauthorizedAccessException("Érvénytelen felhasználó!");

            var ok = user.TwoFactorType == 1 && VerifyTotp(user, dto.Code);
            // email típusnál: ok = await VerifyEmailCodeAsync(user, dto.Code);

            if (!ok && !await TryUseRecoveryCodeAsync(user, dto.Code))
                throw new UnauthorizedAccessException("Érvénytelen kód!");

            return await IssueTokensAsync(user);
        }

        private bool VerifyTotp(AppUser user, string code)
        {
            if (string.IsNullOrEmpty(user.TwoFactorSecret)) return false;

            var secret = _protector.Unprotect(user.TwoFactorSecret);
            var totp = new Totp(Base32Encoding.ToBytes(secret));

            // ±1 időlépés (30 mp) tűrés az óraeltérés miatt
            return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(previous: 1, future: 1));
        }

        public async Task<AuthResponseDto> IssueTokensAsync(AppUser user) 
        {
            string globalToken = _tokenService.GenerateGlobalToken(user);
            string refreshToken = _tokenService.GenerateRefreshToken();
            string refreshTokenHash = HashToken(refreshToken);

            string clientIpAddress = GetClientIpAddress();
            string clientDeviceInfo = GetClientDeviceInfo();
            var now = DateTime.UtcNow;

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == user.Id
                                       && s.DeviceInfo == clientDeviceInfo
                                       && !s.IsRevoked
                                       && s.ExpiresAt > now);

            if (userSession != null)
            {
                userSession.RefreshTokenHash = refreshTokenHash;
                userSession.ExpiresAt = now.AddDays(7);
                userSession.IpAddress = clientIpAddress;
                userSession.UpdatedAt = now;
            }
            else
            {
                userSession = new UserSession
                {
                    UserId = user.Id,
                    RefreshTokenHash = refreshTokenHash,
                    ExpiresAt = now.AddDays(7),
                    IsRevoked = false,
                    CreatedAt = now,
                    IpAddress = clientIpAddress,
                    DeviceInfo = clientDeviceInfo,
                };
                await _context.UserSessions.AddAsync(userSession);
            }

            await _context.SaveChangesAsync();

            return new AuthResponseDto(
                globalToken,
                refreshToken,
                new UserDto(user.Id, user.Name, user.Email, user.CreatedAt)
            );
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        public async Task LogoutAsync(Guid userId, string refreshToken)
        {
            var hash = HashToken(refreshToken);

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(us => us.UserId == userId
                                        && us.RefreshTokenHash == hash
                                        && !us.IsRevoked);

            if (userSession != null)
            {
                userSession.IsRevoked = true;
                userSession.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private static (List<string> Plain, List<string> Hashes) GenerateRecoveryCodes(int count = 10)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // nincs 0/O, 1/I
            var plain = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var chars = new char[8];
                for (int j = 0; j < 8; j++)
                    chars[j] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
                plain.Add($"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}");
            }

            return (plain, plain.Select(HashToken).ToList());
        }

        private async Task<bool> TryUseRecoveryCodeAsync(AppUser user, string code)
        {
            if (string.IsNullOrEmpty(user.TwoFactorRecoveryCodes)) return false;

            var hashes = JsonSerializer.Deserialize<List<string>>(user.TwoFactorRecoveryCodes) ?? new();
            if (!hashes.Remove(HashToken(code.Trim().ToUpperInvariant()))) return false;

            user.TwoFactorRecoveryCodes = JsonSerializer.Serialize(hashes);   // egyszer használható
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TwoFactorSetupDto> BeginTwoFactorSetupAsync(Guid userId)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null)
                ?? throw new UnauthorizedAccessException("Érvénytelen felhasználó!");

            if (user.Is2faEnabled)
                throw new InvalidOperationException("A 2FA már be van kapcsolva!");

            string secret;

            // Ha még nincs aktiválva a 2FA, de már generáltunk secret-et, újrahasznosítjuk
            if (!string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                secret = _protector.Unprotect(user.TwoFactorSecret);
            }
            else
            {
                secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
                user.TwoFactorSecret = _protector.Protect(secret);
                await _context.SaveChangesAsync();
            }

            const string issuer = "PROTOTYPE";
            var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(user.Email)}" +
                      $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

            return new TwoFactorSetupDto(secret, uri);
        }

        public async Task<List<string>> EnableTwoFactorAsync(Guid userId, string code)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null)
                ?? throw new UnauthorizedAccessException("Érvénytelen felhasználó!");

            if (user.Is2faEnabled)
                throw new InvalidOperationException("A 2FA már be van kapcsolva!");

            if (!VerifyTotp(user, code))
                throw new ArgumentException("Érvénytelen kód!");

            var (plain, hashes) = GenerateRecoveryCodes();

            user.Is2faEnabled = true;
            user.TwoFactorType = 1;
            user.TwoFactorRecoveryCodes = JsonSerializer.Serialize(hashes);
            await _context.SaveChangesAsync();

            return plain; 
        }

        public async Task<DisabledTwoFactorResultDto> DisableTwoFactorAsync(Guid userId, string password)
        {
            // 1. Felhasználó lekérése
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new KeyNotFoundException("A felhasználó nem található!");

            // 2. Jelszó ellenőrzése (Hasher segítségével)
            if (string.IsNullOrWhiteSpace(password) || !BCrypt.Net.BCrypt.EnhancedVerify(password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Hibás jelszó!");
            }

            // 3. Ellenőrzés: be van-e egyáltalán kapcsolva?
            if (!user.Is2faEnabled)
                throw new InvalidOperationException("A 2FA nincs bekapcsolva ezen a fiókon!");

            // 4. 2FA beállítások alaphelyzetbe állítása az app_user táblában
            user.Is2faEnabled = false;
            user.TwoFactorType = 0; // 0 = None (enum_lookup alapján)
            user.TwoFactorSecret = null;
            user.TwoFactorRecoveryCodes = null;

            // 5. Az összes meglévő 2FA kód törlése a felhasználóhoz (ha van)
            await _context.User2faCodes
                .Where(c => c.UserId == userId)
                .ExecuteDeleteAsync();

            // 6. Változások mentése (a user frissítéséhez)
            await _context.SaveChangesAsync();

            return new DisabledTwoFactorResultDto(DateTime.UtcNow, user.Is2faEnabled);
        }

        public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
        {
            var hash = HashToken(refreshToken);
            var now = DateTime.UtcNow;

            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.RefreshTokenHash == hash && !s.IsRevoked && s.ExpiresAt > now);

            if (session == null || session.User.DeletedAt != null)
                throw new Exception("Érvénytelen munkamenet!");

            var newRefresh = _tokenService.GenerateRefreshToken();
            session.RefreshTokenHash = HashToken(newRefresh);
            session.ExpiresAt = now.AddDays(7);
            session.UpdatedAt = now;
            await _context.SaveChangesAsync();

            var user = session.User;
            return new AuthResponseDto(
                _tokenService.GenerateGlobalToken(user),
                newRefresh,
                new UserDto(user.Id, user.Name, user.Email, user.CreatedAt));
        }

        public async Task<PermissionResultDtos> GetPermissionsByCatAsync(string query) 
        {
            if(!sbyte.TryParse(query, out sbyte scopeId)) throw new Exception("Nem található a kategória!");
            
            var permissions = await _context.Permissions
                .AsNoTracking()
                .Where(p => p.Scope == scopeId)
                .ToListAsync();

            var result = new PermissionResultDtos
            (
                new Dictionary<sbyte, List<string>> { { scopeId, permissions.Select(p => p.Code).ToList() } }
            );

            return result;
        }

        public async Task<RolesResultDto> GetRolesAsync(string query) 
        {
            if (query == null) throw new Exception("URL tartalma nem megfelelő!");

            var roles = await _context.AppRoles
                .ToListAsync();

            return new RolesResultDto(roles.Select(r => r.Name).ToList());
        }

        public async Task<SpecificRoleResultDto> GetSpecificRoleAsync(Guid roleId) 
        {
            var role = await _context.AppRoles
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null) throw new Exception("A megadott role nem található!");

            return new SpecificRoleResultDto
                (
                    roleId,
                    role.Name,
                    role.Description ?? "",
                    role.IsSystem,
                    role?.CreatedAt
                );
        }

        public async Task<CreateRoleResultDto> CreateRoleAsync(CreateRoleDto dto) 
        {
            var role = await _context.AppRoles
                .FirstOrDefaultAsync(r => r.Id == dto.Id);

            if (role == null) throw new Exception("A megadott role már szerepel!");

            var newRole = new AppRole
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                IsSystem = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.AppRoles.Add(newRole);
            await _context.SaveChangesAsync();

            return new CreateRoleResultDto(newRole.Id, newRole.CreatedAt);
        }

        public async Task<ModifyRoleResultDto> ModifyRoleAsync(Guid Id, ModifyRoleDto dto) 
        {
            var role = await _context.AppRoles
                .FirstOrDefaultAsync(r => r.Id == Id);

            if (role == null) throw new Exception("A megadott role nem található!");

            if (role.IsSystem == false)
            {
                role.Name = dto.Name;
                role.Description = dto.Description;
            }
            else throw new Exception("Az adott role nem módosítható!");
            
            await _context.SaveChangesAsync();

            return new ModifyRoleResultDto(role.Id, role.Name, role.Description);
        }

        public async Task<bool> RemoveRoleAsync(Guid roleId) 
        {
            var role = await _context.AppRoles
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null) throw new Exception("A megadott role nem található!");

            _context.AppRoles.Remove(role);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<AddPermToRoleResultDto> AddPermissionToRoleAsync(Guid roleId, AddPermToRoletDto dto) 
        {
            var role = await _context.AppRoles
                .Include(p => p.Permissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null) throw new Exception("Az adott role nem található!");

            var permissions = await _context.Permissions
                .Where(p => dto.codes.Contains(p.Code))
                .ToListAsync();

            foreach (var permission in permissions) 
            {
                if (role.Permissions.Any(rp => rp.Id == permission.Id)) continue;
                role.Permissions.Add(permission);
            }

            await _context.SaveChangesAsync();

            return new AddPermToRoleResultDto(permissions.Select(p=>p.Code.ToString()).ToList());
        }

        public async Task<bool> CloseSessionAsync(Guid id) 
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(us => us.Id == id);

            if (session == null) throw new Exception("Nem található a munkamenet!");

            session.IsRevoked = true;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> CloseSessionsAsync()
        {
            await _context.UserSessions
                .Where(s => !s.IsRevoked) // Opcionális: csak azokat frissítjük, amik még nincsenek visszavonva
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsRevoked, true)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

            return true;
        }

        public async Task<List<UserSessionsResponseDto>> GetSessionsAsync(int page, int limit) 
        {
            return await _context.UserSessions
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(us => new UserSessionsResponseDto(us.Id, us.UserId, us.DeviceId, us.DeviceInfo, us.IpAddress, us.IsRevoked, us.ExpiresAt, us.UpdatedAt, us.CreatedAt))
                .ToListAsync();
        }

        public async Task RequestPasswordResetAsync(string email)
        {
            // 1. User megkeresése email alapján
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == email);

            // Biztonsági okokból nem dobunk hibát, ha nincs ilyen email
            if (user == null) return;

            // 2. Nyers token generálása (Kryptografiailag biztonságos)
            string rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            // 3. Token lehashelése az adatbázisban való tároláshoz (SHA-256)
            string tokenHash;
            using (var sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(rawToken);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                tokenHash = Convert.ToBase64String(hashBytes);
            }

            // 4. Régi, még fel nem használt tokenek inaktiválása/törlése a felhasználóhoz
            var existingTokens = await _context.PasswordResetTokens
                .Where(prt => prt.UserId == user.Id && !prt.IsUsed)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.IsUsed = true; // A régi tokeneket érvénytelenítjük
            }

            // 5. Új token mentése az adatbázisba (IsUsed = false!)
            var prt = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash, // 👈 Az adatbázisba a HASH kerül
                IsUsed = false,        // 👈 Aktív marad, amíg fel nem használja!
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            _context.PasswordResetTokens.Add(prt);
            await _context.SaveChangesAsync();

            // 6. Link generálása (a NYERS token kerül a linkbe!)
            var baseUrl = _config["FrontendUrl"];
            var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(email)}";

            // 7. E-mail küldése
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, ResetPwdDto dto) 
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return false;

            string hashedToken;
            using (var sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(token);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                hashedToken = Convert.ToBase64String(hashBytes);
            }

            var validToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.TokenHash == hashedToken && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);

            if (validToken == null) return false;

            user.UpdatedAt = DateTime.UtcNow;
            user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(dto.newPwd, 13);
            validToken.IsUsed = true;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}

