// KM

using PROTOTYPE_backend.Data.Repositories;
using PROTOTYPE_backend.DTOs.Auth;
using PROTOTYPE_backend.Models;
using BCrypt.Net;
using PROTOTYPE_backend.Services.Token;

namespace PROTOTYPE_backend.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IHttpContextAccessor httpContextAccessor
            ) 
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
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
            var userExistsByEmail = await _unitOfWork.Repository<AppUser>()
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            var userExistsByName = await _unitOfWork.Repository<AppUser>()
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

            await _unitOfWork.Repository<AppUser>().AddAsync(newUser);
            await _unitOfWork.CompleteAsync();

            return new UserDto(newUser.Id, newUser.Name, newUser.Email, newUser.CreatedAt);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto) 
        {
            var user = await _unitOfWork.Repository<AppUser>()
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(loginDto.Password, user.PasswordHash)) 
            {
                throw new Exception("Érvénytelen e-mail cím vagy jelszó!");
            }

            string globalToken = _tokenService.GenerateGlobalToken(user);
            string refreshToken = _tokenService.GenerateRefreshToken();

            string clientIpAddress = GetClientIpAddress();
            string clientDeviceInfo = GetClientDeviceInfo();

            var userSession = new UserSession
            {
                UserId = user.Id,
                RefreshTokenHash = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                IpAddress = clientIpAddress,
                DeviceInfo = clientDeviceInfo,
            };

            await _unitOfWork.Repository<UserSession>().AddAsync(userSession);
            await _unitOfWork.CompleteAsync();

            return new AuthResponseDto(
                globalToken,
                refreshToken,
                new UserDto(user.Id, user.Name, user.Email, user.CreatedAt)
            );
        }
    }
}

