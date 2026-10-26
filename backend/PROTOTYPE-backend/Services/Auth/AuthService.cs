
// KM

using BCrypt.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Auth;
using PROTOTYPE_backend.Models;
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

        public AuthService(
            ITokenService tokenService,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext context,
            IDataProtectionProvider protector
            ) 
        {
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _protector = protector.CreateProtector("TwoFactorAuth");
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

            var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
            user.TwoFactorSecret = _protector.Protect(secret);
            await _context.SaveChangesAsync();

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
    }
}

