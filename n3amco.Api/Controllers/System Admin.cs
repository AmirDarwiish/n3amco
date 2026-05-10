using n3amco.Api.Users;
using n3amco.Api.Users.Dtos;
using n3amco.Api.Users.DTOs;
using n3amco.Api.Users.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace n3amco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET: api/users
        // =========================
        [HttpGet]
        [Authorize(Policy = "USERS_VIEW")]
        public IActionResult GetAll()
        {
            var users = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.IsActive,
                    Roles = u.UserRoles
                        .Select(ur => ur.Role.Name)
                        .ToList()
                })
                .ToList();

            return Ok(users);
        }

        // =========================
        // GET: api/users/me
        // بيانات المستخدم الحالي — محتاجها الـ Frontend في كل الصفحات
        // لازم تيجي قبل {id} عشان الـ routing ميعملش conflict
        // =========================
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetMe()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return NotFound(new { success = false, message = "User not found" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.IsActive,
                    Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
                }
            });
        }

        // =========================
        // GET: api/users/search?email=xxx
        // GET: api/users/search?name=xxx
        // البحث عن يوزر عشان نضيفه كعضو في بروجكت
        // [Authorize] فقط بدون policy — أي مستخدم مسجل يقدر يبحث
        // لازم تيجي قبل {id} عشان الـ routing ميعملش conflict
        // =========================
        [HttpGet("search")]
        [Authorize]
        public IActionResult Search([FromQuery] string? email, [FromQuery] string? name)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(name))
                return BadRequest(new { success = false, message = "Provide email or name to search" });

            var query = _context.Users.AsNoTracking().Where(u => u.IsActive);

            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(u => u.Email.ToLower() == email.ToLower().Trim());
            else
                query = query.Where(u => u.FullName.ToLower().Contains(name!.ToLower().Trim()));

            var results = query
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email
                })
                .Take(10)
                .ToList();

            if (!results.Any())
                return NotFound(new { success = false, message = "No users found" });

            return Ok(new { success = true, data = results });
        }

        // =========================
        // GET: api/users/{id}
        // =========================
        [HttpGet("{id}")]
        [Authorize(Policy = "USERS_VIEW")]
        public IActionResult GetById(int id)
        {
            var user = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefault(u => u.Id == id);

            if (user == null)
                return NotFound("User not found");

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.IsActive,
                Roles = user.UserRoles
                    .Select(ur => new { ur.Role.Id, ur.Role.Name })
                    .ToList()
            });
        }

        // =========================
        // POST: api/users
        // =========================
        [HttpPost]
        [Authorize(Policy = "USERS_CREATE")]
        public IActionResult CreateUser(CreateUserDto dto)
        {
            if (_context.Users.Any(u => u.Email == dto.Email))
                return BadRequest("Email already exists");

            var role = _context.Roles.FirstOrDefault(r => r.Id == dto.RoleId);
            if (role == null)
                return BadRequest("Invalid role");

            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = hasher.HashPassword(user, dto.Password);
            _context.Users.Add(user);
            _context.SaveChanges();

            _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            _context.SaveChanges();

            return Ok("User created successfully");
        }

        // =========================
        // PUT: api/users/change-role
        // لازم تيجي قبل {id} عشان الـ routing ميعملش conflict
        // =========================
        [HttpPut("change-role")]
        [Authorize(Policy = "USERS_CHANGE_ROLE")]
        public IActionResult ChangeUserRole(ChangeUserRoleDto dto)
        {
            var user = _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefault(u => u.Id == dto.UserId);

            if (user == null)
                return NotFound("User not found");

            var role = _context.Roles.FirstOrDefault(r => r.Id == dto.RoleId);
            if (role == null)
                return BadRequest("Invalid role");

            _context.UserRoles.RemoveRange(user.UserRoles);
            _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            _context.SaveChanges();

            return Ok("Role updated successfully");
        }

        // =========================
        // PUT: api/users/{id}
        // =========================
        [HttpPut("{id}")]
        [Authorize(Policy = "USERS_EDIT")]
        public IActionResult Update(int id, UpdateUserDto dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound("User not found");

            if (string.IsNullOrWhiteSpace(dto.FullName))
                return BadRequest("Full name is required");

            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("Email is required");

            if (_context.Users.Any(u => u.Email == dto.Email && u.Id != id))
                return BadRequest("Email already exists");

            user.FullName = dto.FullName.Trim();
            user.Email = dto.Email.Trim();
            user.IsActive = dto.IsActive;
            _context.SaveChanges();

            return Ok("User updated successfully");
        }

        // =========================
        // PUT: api/users/{id}/status
        // =========================
        [HttpPut("{id}/status")]
        [Authorize(Policy = "USERS_CHANGE_STATUS")]
        public IActionResult UpdateUserStatus(int id, [FromBody] bool isActive)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound("User not found");

            if (user.IsActive == isActive)
                return BadRequest("User already in this state");

            user.IsActive = isActive;
            _context.SaveChanges();

            return Ok(new { user.Id, user.IsActive });
        }

        // =========================
        // PUT: api/users/{id}/reset-password
        // =========================
        [HttpPut("{id}/reset-password")]
        [Authorize(Policy = "USERS_RESET_PASSWORD")]
        public IActionResult ResetPassword(int id, ResetUserPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("Password is required");

            if (dto.NewPassword.Length < 6)
                return BadRequest("Password must be at least 6 characters");

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound("User not found");

            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, dto.NewPassword);
            _context.SaveChanges();

            return Ok("Password reset successfully");
        }
    }
}