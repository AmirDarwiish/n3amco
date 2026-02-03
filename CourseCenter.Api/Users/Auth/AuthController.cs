using CourseCenter.Api.Users;
using CourseCenter.Api.Users.Auth;
using CourseCenter.Api.Users.Permissions;
using CourseCenter.Api.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CourseCenter.Api.Users.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserActivityLogger _userActivityLogger; 
        private readonly IConfiguration _config;

        public AuthController(
     ApplicationDbContext context,
     IConfiguration config,
     IUserActivityLogger userActivityLogger)
        {
            _context = context;
            _config = config;
            _userActivityLogger = userActivityLogger;
        }


        // =========================
        // LOGIN
        // =========================
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefault(u => u.Email == request.Email && u.IsActive);

            if (user == null)
                return Unauthorized("Invalid credentials");

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password
            );

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid credentials");

            var accessToken = GenerateAccessToken(user);

            var refreshToken = GenerateRefreshToken(user.Id);
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            SetRefreshCookie(refreshToken);

            // ✅ تسجيل Login Activity
            await _userActivityLogger.LogAsync(user.Id, "Login");

            var roles = user.UserRoles
                .Select(r => r.Role.Name)
                .ToList();

            var permissions = _context.RolePermissions
                .Where(rp => roles.Contains(rp.Role))
                .Select(rp => rp.Permission.Code)
                .Distinct()
                .ToList();

            return Ok(new LoginResponse
            {
                Token = accessToken,
                FullName = user.FullName,
                Email = user.Email,
                Roles = roles,
                Permissions = permissions
            });
        }


        // =========================
        // REFRESH
        // =========================
        [HttpPost("refresh")]
        [AllowAnonymous]
        public IActionResult Refresh([FromBody] RefreshRequest? body)
        {
            // try cookie first, then x-refresh-token header, then Authorization header (bearer)
            string? token = null;

            if (Request.Cookies.TryGetValue("refreshToken", out var cookieToken))
                token = cookieToken;

            if (string.IsNullOrEmpty(token) && Request.Headers.TryGetValue("x-refresh-token", out var hdr))
                token = hdr.ToString();

            if (string.IsNullOrEmpty(token) && Request.Headers.TryGetValue("Authorization", out var auth))
            {
                var authVal = auth.ToString();
                if (authVal.StartsWith("Bearer "))
                    token = authVal.Substring("Bearer ".Length).Trim();
            }

            // also allow refresh token in request body (for clients that don't use cookies)
            if (string.IsNullOrEmpty(token) && body != null && !string.IsNullOrWhiteSpace(body.RefreshToken))
                token = body.RefreshToken.Trim();

            if (string.IsNullOrEmpty(token))
                return Unauthorized("No refresh token");

            var refreshToken = _context.RefreshTokens
                .Include(r => r.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .FirstOrDefault(r =>
                    r.Token == token &&
                    !r.IsRevoked &&
                    r.ExpiresAt > DateTime.UtcNow
                );

            if (refreshToken == null)
                return Unauthorized("Invalid refresh token");

            // 🔁 ROTATION
            refreshToken.IsRevoked = true;

            var newRefreshToken = GenerateRefreshToken(refreshToken.UserId);
            _context.RefreshTokens.Add(newRefreshToken);

            // Load fresh user from DB to ensure roles/permissions are up-to-date
            var user = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefault(u => u.Id == refreshToken.UserId);

            if (user == null)
                return Unauthorized("Invalid refresh token user");

            var newAccessToken = GenerateAccessToken(user);

            _context.SaveChanges();

            SetRefreshCookie(newRefreshToken);

            return Ok(new
            {
                accessToken = newAccessToken
            });
        }

        public class RefreshRequest
        {
            public string? RefreshToken { get; set; }
        }

        // =========================
        // LOGOUT
        // =========================
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            if (Request.Cookies.TryGetValue("refreshToken", out var token))
            {
                var refreshToken = _context.RefreshTokens
                    .FirstOrDefault(r => r.Token == token);

                if (refreshToken != null)
                {
                    refreshToken.IsRevoked = true;
                    _context.SaveChanges();
                }
            }

            Response.Cookies.Delete("refreshToken");
            return Ok();
        }

        // =========================
        // ACCESS TOKEN
        // =========================
        private string GenerateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName)
            };

            // Load roles for the user from the database to ensure correctness in refresh flow
            List<string> roles = new List<string>();
            try
            {
                roles = _context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Include(ur => ur.Role)
                    .Select(ur => ur.Role.Name)
                    .ToList();

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var permissionCodes = _context.RolePermissions
                    .Where(rp => roles.Contains(rp.Role))
                    .Select(rp => rp.Permission.Code)
                    .Distinct()
                    .ToList();

                foreach (var code in permissionCodes)
                {
                    if (string.IsNullOrWhiteSpace(code))
                        continue;

                    claims.Add(new Claim("permission", code.Trim()));
                }
            }
            catch
            {
                // if permissions/roles can't be loaded here, do not fail token generation
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // =========================
        // REFRESH TOKEN
        // =========================
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

        // =========================
        // COOKIE
        // =========================
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
