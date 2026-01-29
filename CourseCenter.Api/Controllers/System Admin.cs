using CourseCenter.Api.Users;
using CourseCenter.Api.Users.Dtos;
using CourseCenter.Api.Users.DTOs;
using CourseCenter.Api.Users.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseCenter.Api.Controllers
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
        // POST: api/users
        // Add Employee
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

            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            _context.SaveChanges();

            return Ok("User created successfully");
        }

        // =========================
        // PUT: api/users/change-role
        // Change User Role
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

            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            _context.SaveChanges();

            return Ok("Role updated successfully");
        }

        // =========================
        // PUT: api/users/{id}/status
        // Update User Status (Activate / Disable)
        // ======================================
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

            return Ok(new
            {
                user.Id,
                user.IsActive
            });
        }

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

            var response = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.IsActive,
                Roles = user.UserRoles
                    .Select(ur => new
                    {
                        ur.Role.Id,
                        ur.Role.Name
                    })
                    .ToList()
            };

            return Ok(response);
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

            var emailExists = _context.Users.Any(u =>
                u.Email == dto.Email && u.Id != id);

            if (emailExists)
                return BadRequest("Email already exists");

            user.FullName = dto.FullName.Trim();
            user.Email = dto.Email.Trim();
            user.IsActive = dto.IsActive;

            _context.SaveChanges();

            return Ok("User updated successfully");
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
