using DairySystem.Api.Common;
using DairySystem.Api.Users;
using DairySystem.Api.Users.Auth;
using DairySystem.Api.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DairySystem.Api.Users.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IUserActivityLogger _logger;
        private readonly IMemoryCache _cache;

        public AuthController(
            ApplicationDbContext context,
            IConfiguration config,
            IUserActivityLogger logger,
            IMemoryCache cache)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _cache = cache;
        }

        // ========================= LOGIN =========================
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == request.Email && x.IsActive);

            if (user == null)
                throw new AppException("AUTH_INVALID_CREDENTIALS", 401);

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (result == PasswordVerificationResult.Failed)
                throw new AppException("AUTH_INVALID_CREDENTIALS", 401);

            // 🧠 Roles
            var roles = await _context.UserRoles
                .Where(x => x.UserId == user.Id)
                .Include(x => x.Role)
                .Select(x => x.Role.Name)
                .ToListAsync();

            // 🧠 RoleIds
            var roleIds = await _context.UserRoles
                .Where(x => x.UserId == user.Id)
                .Select(x => x.RoleId)
                .ToListAsync();

            // 🔥 Permissions (SAFE + CACHE)
            var cacheKey = $"permissions_user_{user.Id}";

            if (!_cache.TryGetValue(cacheKey, out List<string> permissions))
            {
                permissions = _context.RolePermissions
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.Role != null)
                    .AsEnumerable() // 👈 عشان TryParse
                    .Where(rp =>
                    {
                        if (int.TryParse(rp.Role, out var roleId))
                            return roleIds.Contains(roleId);

                        return false;
                    })
                    .Select(rp => rp.Permission.Code)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                _cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(10));
            }

            var token = GenerateAccessToken(user, roles, permissions);

            var refreshToken = GenerateRefreshToken(user.Id);
            _context.RefreshTokens.Add(refreshToken);

            await _context.SaveChangesAsync();
            SetRefreshCookie(refreshToken);

            await _logger.LogAsync(user.Id, "Login");

            return Ok(new LoginResponse
            {
                Token = token,
                FullName = user.FullName,
                Email = user.Email,
                id = user.Id.ToString(),
                Roles = roles,
                Permissions = permissions
            });
        }

        // ========================= REFRESH =========================
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out var token))
                throw new AppException("AUTH_REFRESH_TOKEN_MISSING", 401);

            var refreshToken = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r =>
                    r.Token == token &&
                    !r.IsRevoked &&
                    r.ExpiresAt > DateTime.UtcNow);

            if (refreshToken == null)
                throw new AppException("AUTH_INVALID_REFRESH_TOKEN", 401);

            refreshToken.IsRevoked = true;

            var user = await _context.Users.FindAsync(refreshToken.UserId);
            if (user == null)
                throw new AppException("AUTH_INVALID_CREDENTIALS", 401);

            var roles = await _context.UserRoles
                .Where(x => x.UserId == user.Id)
                .Include(x => x.Role)
                .Select(x => x.Role.Name)
                .ToListAsync();

            var roleIds = await _context.UserRoles
                .Where(x => x.UserId == user.Id)
                .Select(x => x.RoleId)
                .ToListAsync();

            var cacheKey = $"permissions_user_{user.Id}";

            if (!_cache.TryGetValue(cacheKey, out List<string> permissions))
            {
                permissions = _context.RolePermissions
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.Role != null)
                    .AsEnumerable()
                    .Where(rp =>
                    {
                        if (int.TryParse(rp.Role, out var roleId))
                            return roleIds.Contains(roleId);

                        return false;
                    })
                    .Select(rp => rp.Permission.Code)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                _cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(10));
            }

            var newAccessToken = GenerateAccessToken(user, roles, permissions);

            var newRefreshToken = GenerateRefreshToken(user.Id);
            _context.RefreshTokens.Add(newRefreshToken);

            await _context.SaveChangesAsync();
            SetRefreshCookie(newRefreshToken);

            return Ok(new { accessToken = newAccessToken });
        }

        // ========================= LOGOUT =========================
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue("refreshToken", out var token))
            {
                var refresh = await _context.RefreshTokens
                    .FirstOrDefaultAsync(x => x.Token == token);

                if (refresh != null)
                {
                    refresh.IsRevoked = true;
                    await _context.SaveChangesAsync();
                }
            }

            Response.Cookies.Delete("refreshToken");
            return Ok();
        }

        // ========================= JWT =========================
        private string GenerateAccessToken(User user, List<string> roles, List<string> permissions)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName)
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            foreach (var permission in permissions)
                claims.Add(new Claim("permission", permission));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ========================= REFRESH TOKEN =========================
        private RefreshToken GenerateRefreshToken(int userId)
        {
            return new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(14),
                IsRevoked = false
            };
        }

        private void SetRefreshCookie(RefreshToken token)
        {
            Response.Cookies.Append("refreshToken", token.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = token.ExpiresAt
            });
        }
    }
}