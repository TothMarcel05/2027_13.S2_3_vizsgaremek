// KM

using BCrypt.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ITokenService tokenService,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context,
            IDataProtectionProvider protector,
            IConfiguration config,
            IEmailService emailService,
            ILogger<AuthService> logger
            )
        {
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _protector = protector.CreateProtector("TwoFactorAuth");
            _config = config;
            _emailService = emailService;
            _logger = logger;
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
                _logger.LogWarning("Sikertelen regisztráció: az e-mail cím már foglalt. IP: {IpAddress}", GetClientIpAddress());
                throw new InvalidOperationException("Az email cím már foglalt!");
            }

            if (userExistsByName != null)
            {
                _logger.LogWarning("Sikertelen regisztráció: a felhasználónév már foglalt. Username: {Username}, IP: {IpAddress}", dto.Username, GetClientIpAddress());
                throw new InvalidOperationException("A felhasználó név már foglalt!");
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

            _logger.LogInformation("Új felhasználó regisztrált. UserId: {UserId}, IP: {IpAddress}", newUser.Id, GetClientIpAddress());

            return new UserDto(newUser.Id, newUser.Name, newUser.Email, newUser.CreatedAt);
        }

        public async Task<LoginResultDto> LoginAsync(LoginDto loginDto)
        {
            var ip = GetClientIpAddress();
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.DeletedAt == null);

            if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Sikertelen bejelentkezési kísérlet. UserId: {UserId}, IP: {IpAddress}", user?.Id, ip);
                throw new UnauthorizedAccessException("Érvénytelen e-mail cím vagy jelszó!");
            }

            if (user.IsActive == false)
            {
                _logger.LogWarning("Felfüggesztett fiók bejelentkezési kísérlete. UserId: {UserId}, IP: {IpAddress}", user.Id, ip);
                throw new UnauthorizedAccessException("A felhasználói fiók fel van függesztve!");
            }

            if (user.Is2faEnabled)
            {
                if (user.TwoFactorType == 2) await SendEmailCodeAsync(user);
                _logger.LogInformation("Sikeres jelszó-ellenőrzés, 2FA lépés szükséges. UserId: {UserId}, TwoFactorType: {TwoFactorType}, IP: {IpAddress}",
                    user.Id, user.TwoFactorType, ip);
                return new LoginResultDto(true, user.TwoFactorType, _tokenService.GenerateTwoFactorToken(user), null);
            }

            _logger.LogInformation("Sikeres bejelentkezés (2FA nélkül). UserId: {UserId}, IP: {IpAddress}", user.Id, ip);
            return new LoginResultDto(false, null, null, await IssueTokensAsync(user));
        }

        public async Task<AuthResponseDto> VerifyTwoFactorAsync(TwoFactorLoginDto dto)
        {
            var ip = GetClientIpAddress();
            var userId = _tokenService.ValidateTwoFactorToken(dto.TwoFactorToken);
            if (userId == null)
            {
                _logger.LogWarning("Sikertelen 2FA ellenőrzés: érvénytelen vagy lejárt 2FA token. IP: {IpAddress}", ip);
                throw new UnauthorizedAccessException("Lejárt vagy érvénytelen 2FA munkamenet!");
            }

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive == true && u.Is2faEnabled);
            if (user == null)
            {
                _logger.LogWarning("Sikertelen 2FA ellenőrzés: nem létező vagy inaktív felhasználó. UserId: {UserId}, IP: {IpAddress}", userId, ip);
                throw new UnauthorizedAccessException("Érvénytelen felhasználó!");
            }

            bool ok = user.TwoFactorType switch
            {
                1 => VerifyTotp(user, dto.Code),
                2 => await VerifyEmailCodeAsync(user, dto.Code),
                _ => false
            };

            if (!ok && !await TryUseRecoveryCodeAsync(user, dto.Code))
            {
                _logger.LogWarning("Sikertelen 2FA kód ellenőrzés! UserId: {UserId}, TwoFactorType: {TwoFactorType}, IP: {IpAddress}",
                    user.Id, user.TwoFactorType, ip);
                throw new UnauthorizedAccessException("Érvénytelen kód!");
            }

            _logger.LogInformation("Sikeres 2FA hitelesítés. UserId: {UserId}, IP: {IpAddress}", user.Id, ip);
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
                _logger.LogDebug("Meglévő munkamenet frissítve. SessionId: {SessionId}, UserId: {UserId}", userSession.Id, user.Id);
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
                _logger.LogInformation("Új munkamenet létrehozva. UserId: {UserId}, IP: {IpAddress}", user.Id, clientIpAddress);
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
                _logger.LogInformation("Felhasználó kijelentkezett, munkamenet visszavonva. UserId: {UserId}, SessionId: {SessionId}", userId, userSession.Id);
            }
            else
            {
                _logger.LogDebug("Kijelentkezés: nem található aktív munkamenet a megadott tokenhez. UserId: {UserId}", userId);
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

            _logger.LogWarning("2FA helyreállító kód (recovery code) felhasználva! UserId: {UserId}, Maradék kódok száma: {RemainingCount}, IP: {IpAddress}",
                user.Id, hashes.Count, GetClientIpAddress());

            return true;
        }

        public async Task<TwoFactorSetupDto> BeginTwoFactorSetupAsync(Guid userId)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive == true)
                    ?? throw new UnauthorizedAccessException("Érvénytelen vagy felfüggesztett felhasználó!");

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
                _logger.LogInformation("Új TOTP 2FA titkos kulcs generálva a beállításhoz. UserId: {UserId}", userId);
            }

            const string issuer = "PROTOTYPE";
            var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(user.Email)}" +
                      $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

            return new TwoFactorSetupDto(secret, uri);
        }

        public async Task<List<string>> EnableTwoFactorAsync(Guid userId, string code)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive == true)
                    ?? throw new UnauthorizedAccessException("Érvénytelen vagy felfüggesztett felhasználó!");

            if (user.Is2faEnabled)
                throw new InvalidOperationException("A 2FA már be van kapcsolva!");

            if (!VerifyTotp(user, code))
            {
                _logger.LogWarning("TOTP 2FA aktiválás sikertelen: érvénytelen visszaigazoló kód. UserId: {UserId}", userId);
                throw new ArgumentException("Érvénytelen kód!");
            }

            var (plain, hashes) = GenerateRecoveryCodes();

            user.Is2faEnabled = true;
            user.TwoFactorType = 1;
            user.TwoFactorRecoveryCodes = JsonSerializer.Serialize(hashes);
            await _context.SaveChangesAsync();

            _logger.LogInformation("TOTP 2FA sikeresen aktiválva. UserId: {UserId}", userId);

            return plain;
        }

        public async Task<DisabledTwoFactorResultDto> DisableTwoFactorAsync(Guid userId, string password)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new KeyNotFoundException("A felhasználó nem található!");

            if (string.IsNullOrWhiteSpace(password) || !BCrypt.Net.BCrypt.EnhancedVerify(password, user.PasswordHash))
            {
                _logger.LogWarning("2FA kikapcsolás elutasítva: hibás jelszó. UserId: {UserId}, IP: {IpAddress}", userId, GetClientIpAddress());
                throw new UnauthorizedAccessException("Hibás jelszó!");
            }

            if (!user.Is2faEnabled)
                throw new InvalidOperationException("A 2FA nincs bekapcsolva ezen a fiókon!");

            user.Is2faEnabled = false;
            user.TwoFactorType = 0; // 0 = None (enum_lookup alapján)
            user.TwoFactorSecret = null;
            user.TwoFactorRecoveryCodes = null;

            await _context.User2faCodes
                .Where(c => c.UserId == userId)
                .ExecuteDeleteAsync();

            await _context.SaveChangesAsync();

            _logger.LogWarning("2FA kikapcsolva a fiókon! UserId: {UserId}, IP: {IpAddress}", userId, GetClientIpAddress());

            return new DisabledTwoFactorResultDto(DateTime.UtcNow, user.Is2faEnabled);
        }

        private async Task SendEmailCodeAsync(AppUser user)
        {
            await _context.User2faCodes
                .Where(c => c.UserId == user.Id && !c.IsUsed)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsUsed, true));

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var codeHash = HashToken(code);

            var entry = new User2faCode
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CodeHash = codeHash,
                FailedAttempts = 0,
                MaxAttempts = 5,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.User2faCodes.Add(entry);
            await _context.SaveChangesAsync();

            await _emailService.SendTwoFactorCodeEmailAsync(user.Email, code);
            _logger.LogInformation("E-mailes 2FA kód generálva és kiküldve. UserId: {UserId}", user.Id);
        }

        private async Task<bool> VerifyEmailCodeAsync(AppUser user, string code)
        {
            var now = DateTime.UtcNow;
            var activeCode = await _context.User2faCodes
                .Where(c => c.UserId == user.Id && !c.IsUsed && c.ExpiresAt > now)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (activeCode == null)
            {
                _logger.LogWarning("E-mailes 2FA kód ellenőrzés sikertelen: nincs aktív vagy nem lejárt kód. UserId: {UserId}", user.Id);
                return false;
            }

            if (activeCode.CodeHash != HashToken(code.Trim()))
            {
                activeCode.FailedAttempts++;
                if (activeCode.FailedAttempts >= activeCode.MaxAttempts)
                {
                    activeCode.IsUsed = true;
                    _logger.LogWarning("E-mailes 2FA kód zárolva a maximális hibás próbálkozások ({MaxAttempts}) elérése miatt! UserId: {UserId}, IP: {IpAddress}",
                        activeCode.MaxAttempts, user.Id, GetClientIpAddress());
                }
                else
                {
                    _logger.LogWarning("Hibás e-mailes 2FA kód ({Attempts}/{MaxAttempts}). UserId: {UserId}, IP: {IpAddress}",
                        activeCode.FailedAttempts, activeCode.MaxAttempts, user.Id, GetClientIpAddress());
                }

                await _context.SaveChangesAsync();
                return false;
            }

            activeCode.IsUsed = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> EnableEmailTwoFactorAsync(Guid userId, string password)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive == true)
                    ?? throw new UnauthorizedAccessException("Érvénytelen vagy felfüggesztett felhasználó!");

            if (user.Is2faEnabled)
                throw new InvalidOperationException("A 2FA már be van kapcsolva!");

            if (!BCrypt.Net.BCrypt.EnhancedVerify(password, user.PasswordHash))
            {
                _logger.LogWarning("E-mailes 2FA aktiválás elutasítva: hibás jelszó. UserId: {UserId}", userId);
                throw new UnauthorizedAccessException("Hibás jelszó!");
            }

            var (plain, hashes) = GenerateRecoveryCodes();

            user.Is2faEnabled = true;
            user.TwoFactorType = 2; // 2 = Email (enum_lookup alapján)
            user.TwoFactorSecret = null;
            user.TwoFactorRecoveryCodes = JsonSerializer.Serialize(hashes);

            await _context.SaveChangesAsync();

            _logger.LogInformation("E-mailes 2FA sikeresen aktiválva. UserId: {UserId}", userId);

            return plain;
        }

        public async Task<List<string>> RegenerateRecoveryCodesAsync(Guid userId, string password)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive == true)
                    ?? throw new UnauthorizedAccessException("Érvénytelen vagy felfüggesztett felhasználó!");

            if (!user.Is2faEnabled)
                throw new InvalidOperationException("A 2FA nincs bekapcsolva!");

            if (!BCrypt.Net.BCrypt.EnhancedVerify(password, user.PasswordHash))
            {
                _logger.LogWarning("Recovery kód újragenerálás elutasítva: hibás jelszó. UserId: {UserId}", userId);
                throw new UnauthorizedAccessException("Hibás jelszó!");
            }

            var (plain, hashes) = GenerateRecoveryCodes();
            user.TwoFactorRecoveryCodes = JsonSerializer.Serialize(hashes);

            await _context.SaveChangesAsync();

            _logger.LogInformation("2FA helyreállító kódok újragenerálva. UserId: {UserId}", userId);

            return plain;
        }

        public async Task ResendTwoFactorEmailCodeAsync(string twoFactorToken)
        {
            var userId = _tokenService.ValidateTwoFactorToken(twoFactorToken)
                ?? throw new UnauthorizedAccessException("Lejárt vagy érvénytelen 2FA munkamenet!");

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null && u.IsActive == true && u.Is2faEnabled && u.TwoFactorType == 2)
                    ?? throw new InvalidOperationException("Ehhez a fiókhoz nem kérhető e-mailes 2FA kód!");

            var recentCode = await _context.User2faCodes
                .Where(c => c.UserId == user.Id && c.CreatedAt > DateTime.UtcNow.AddSeconds(-30))
                .AnyAsync();

            if (recentCode)
            {
                _logger.LogWarning("E-mailes 2FA kód újraküldés elutasítva (rate limit). UserId: {UserId}, IP: {IpAddress}", user.Id, GetClientIpAddress());
                throw new InvalidOperationException("Kérjük, várj 30 másodpercet az újraküldés előtt!");
            }

            await SendEmailCodeAsync(user);
        }

        public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
        {
            var hash = HashToken(refreshToken);
            var now = DateTime.UtcNow;

            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.RefreshTokenHash == hash && !s.IsRevoked && s.ExpiresAt > now);

            if (session == null || session.User.DeletedAt != null || session.User.IsActive == false)
            {
                _logger.LogWarning("Sikertelen token frissítés: érvénytelen, felfüggesztett vagy visszavont munkamenet. IP: {IpAddress}", GetClientIpAddress());
                throw new UnauthorizedAccessException("Érvénytelen munkamenet!");
            }

            var newRefresh = _tokenService.GenerateRefreshToken();
            session.RefreshTokenHash = HashToken(newRefresh);
            session.ExpiresAt = now.AddDays(7);
            session.UpdatedAt = now;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Refresh token rotálva. UserId: {UserId}, SessionId: {SessionId}", session.UserId, session.Id);

            var user = session.User;
            return new AuthResponseDto(
                _tokenService.GenerateGlobalToken(user),
                newRefresh,
                new UserDto(user.Id, user.Name, user.Email, user.CreatedAt));
        }

        public async Task<PermissionResultDtos> GetPermissionsByCatAsync(string query)
        {
            if (!sbyte.TryParse(query, out sbyte scopeId)) throw new ArgumentException("Nem található a kategória!");

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
            if (query == null) throw new ArgumentException("URL tartalma nem megfelelő!");

            var roles = await _context.AppRoles
                .AsNoTracking()
                .ToListAsync();

            return new RolesResultDto(roles.Select(r => r.Name).ToList());
        }

        public async Task<SpecificRoleResultDto> GetSpecificRoleAsync(Guid roleId)
        {
            var role = await _context.AppRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null) throw new KeyNotFoundException("A megadott role nem található!");

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

            if (role != null) throw new InvalidOperationException("A megadott role már szerepel!");

            var nameTaken = await _context.AppRoles.AnyAsync(r => r.Name == dto.Name);
            if (nameTaken) throw new InvalidOperationException("A megadott nevű role már szerepel!");

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

            _logger.LogInformation("Új szerepkör létrehozva az AuthService-ben. RoleId: {RoleId}, RoleName: {RoleName}", newRole.Id, newRole.Name);

            return new CreateRoleResultDto(newRole.Id, newRole.CreatedAt);
        }

        public async Task<ModifyRoleResultDto> ModifyRoleAsync(Guid Id, ModifyRoleDto dto)
        {
            var role = await _context.AppRoles
                .FirstOrDefaultAsync(r => r.Id == Id);

            if (role == null) throw new KeyNotFoundException("A megadott role nem található!");

            if (role.IsSystem == false)
            {
                role.Name = dto.Name;
                role.Description = dto.Description;
            }
            else
            {
                _logger.LogWarning("Beépített rendszerszerepkör módosítási kísérlet elutasítva. RoleId: {RoleId}", Id);
                throw new ArgumentException("Az adott role nem módosítható!");
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Szerepkör módosítva. RoleId: {RoleId}, Új név: {RoleName}", role.Id, role.Name);

            return new ModifyRoleResultDto(role.Id, role.Name, role.Description);
        }

        public async Task<bool> RemoveRoleAsync(Guid roleId)
        {
            var role = await _context.AppRoles
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null) throw new KeyNotFoundException("A megadott role nem található!");

            _context.AppRoles.Remove(role);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Szerepkör törölve. RoleId: {RoleId}, RoleName: {RoleName}", roleId, role.Name);

            return true;
        }

        public async Task<AddPermToRoleResultDto> AddPermissionToRoleAsync(Guid roleId, AddPermToRoletDto dto)
        {
            var role = await _context.AppRoles
                .Include(p => p.Permissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null) throw new KeyNotFoundException("Az adott role nem található!");

            var permissions = await _context.Permissions
                .Where(p => dto.codes.Contains(p.Code))
                .ToListAsync();

            foreach (var permission in permissions)
            {
                if (role.Permissions.Any(rp => rp.Id == permission.Id)) continue;
                role.Permissions.Add(permission);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Jogosultságok hozzáadva szerepkörhöz. RoleId: {RoleId}, Hozzáadott kódok: {@Permissions}",
                roleId, permissions.Select(p => p.Code).ToList());

            return new AddPermToRoleResultDto(permissions.Select(p => p.Code.ToString()).ToList());
        }

        public async Task<bool> CloseSessionAsync(Guid id)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(us => us.Id == id);

            if (session == null) throw new KeyNotFoundException("Nem található a munkamenet!");

            session.IsRevoked = true;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Munkamenet lezárva. SessionId: {SessionId}, UserId: {UserId}", id, session.UserId);

            return true;
        }

        public async Task<bool> CloseSessionsAsync()
        {
            var affected = await _context.UserSessions
                .Where(s => !s.IsRevoked)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsRevoked, true)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

            _logger.LogWarning("Minden aktív munkamenet globálisan visszavonva! Lezárt munkamenetek száma: {Count}", affected);

            return true;
        }

        public async Task<List<UserSessionsResponseDto>> GetSessionsAsync(int page, int limit)
        {
            return await _context.UserSessions
                .AsNoTracking()
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(us => new UserSessionsResponseDto(us.Id, us.UserId, us.DeviceId, us.DeviceInfo, us.IpAddress, us.IsRevoked, us.ExpiresAt, us.UpdatedAt, us.CreatedAt))
                .ToListAsync();
        }

        public async Task RequestPasswordResetAsync(string email)
        {
            var ip = GetClientIpAddress();
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                _logger.LogDebug("Jelszó-visszaállítási kérelem nem létező e-mail címre. IP: {IpAddress}", ip);
                return;
            }

            string rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            string tokenHash;
            using (var sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(rawToken);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                tokenHash = Convert.ToBase64String(hashBytes);
            }

            var existingTokens = await _context.PasswordResetTokens
                .Where(prt => prt.UserId == user.Id && !prt.IsUsed)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.IsUsed = true;
            }

            var prt = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            _context.PasswordResetTokens.Add(prt);
            await _context.SaveChangesAsync();

            var baseUrl = _config["FrontendUrl"];
            var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(email)}";

            await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

            _logger.LogInformation("Jelszó-visszaállító e-mail kiküldve. UserId: {UserId}, IP: {IpAddress}", user.Id, ip);
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, ResetPwdDto dto)
        {
            var ip = GetClientIpAddress();
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                _logger.LogWarning("Jelszó-visszaállítás sikertelen: nem létező felhasználó. IP: {IpAddress}", ip);
                return false;
            }

            string hashedToken;
            using (var sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(token);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                hashedToken = Convert.ToBase64String(hashBytes);
            }

            var validToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.TokenHash == hashedToken && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);

            if (validToken == null)
            {
                _logger.LogWarning("Jelszó-visszaállítás sikertelen: érvénytelen vagy lejárt reset token. UserId: {UserId}, IP: {IpAddress}", user.Id, ip);
                return false;
            }

            user.UpdatedAt = DateTime.UtcNow;
            user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(dto.newPwd, 13);
            validToken.IsUsed = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Felhasználó jelszava sikeresen megváltoztatva (reset). UserId: {UserId}, IP: {IpAddress}", user.Id, ip);

            return true;
        }
    }
}