using n3amco.Api.Users.Dtos;
using n3amco.Api.Users.Permissions;
using n3amco.Api.Users.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace n3amco.Api.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RolesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // PUT: api/roles/{roleId}
        // Update role name and its permissions atomically
        // =========================
        [HttpPut("{roleId}")]
        [Authorize(Policy = "ROLES_EDIT")]
        public IActionResult UpdateRole(int roleId, [FromBody] Users.Dtos.UpdateRoleDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.RoleName))
                return BadRequest("RoleName is required");

            var newRoleName = dto.RoleName.Trim();

            // normalize and dedupe requested permission codes
            var requestedCodes = dto.PermissionCodes?
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            var role = _context.Roles.FirstOrDefault(r => r.Id == roleId);
            if (role == null)
                return NotFound("Role not found");

            // start transaction
            using var tx = _context.Database.BeginTransaction();
            try
            {
                // validate permissions exist
                var requestedSet = new HashSet<string>(requestedCodes, StringComparer.OrdinalIgnoreCase);

                var permissions = _context.Permissions
                    .Where(p => requestedSet.Contains(p.Code))
                    .ToList();

                var foundSet = new HashSet<string>(permissions.Select(p => p.Code), StringComparer.OrdinalIgnoreCase);
                var missing = requestedCodes.Where(c => !foundSet.Contains(c)).ToList();
                if (missing.Any())
                {
                    return BadRequest(new { message = "Some permission codes are invalid", missing });
                }

                var oldRoleName = role.Name;

                // update role name
                role.Name = newRoleName;
                _context.SaveChanges();

                // remove existing role-permissions for the old role name
                var existingRPs = _context.RolePermissions.Where(rp => rp.Role == oldRoleName).ToList();
                if (existingRPs.Any())
                {
                    _context.RolePermissions.RemoveRange(existingRPs);
                    _context.SaveChanges();
                }

                // add new role-permissions
                var toAdd = permissions.Select(p => new RolePermission
                {
                    Role = newRoleName,
                    PermissionId = p.Id
                }).ToList();

                if (toAdd.Any())
                {
                    _context.RolePermissions.AddRange(toAdd);
                    _context.SaveChanges();
                }

                tx.Commit();

                return Ok(new
                {
                    roleId = role.Id,
                    roleName = role.Name,
                    permissions = permissions.Select(p => p.Code).ToList()
                });
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // =========================
        // GET: api/roles
        // =========================
        [HttpGet]
        [Authorize(Policy = "ROLES_VIEW")]
        public IActionResult GetAll()
        {
            var roles = _context.Roles
                .Select(r => new
                {
                    r.Id,
                    r.Name
                })
                .ToList();

            return Ok(roles);
        }

        // =========================
        // GET: api/roles/{roleId}/permissions
        // =========================
        [HttpGet("{roleId}/permissions")]
        [Authorize(Policy = "ROLES_PERMISSIONS_VIEW")]
        public IActionResult GetRolePermissions(int roleId)
        {
            var role = _context.Roles.FirstOrDefault(r => r.Id == roleId);
            if (role == null)
                return NotFound("Role not found");

            var permissions = _context.RolePermissions
                .Where(rp => rp.Role == role.Name)
                .Include(rp => rp.Permission)
                .Select(rp => new
                {
                    Code = rp.Permission.Code,
                    Name = rp.Permission.Name,
                    Module = rp.Permission.Module
                })
                .ToList();

            return Ok(new
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Permissions = permissions
            });
        }


        // =========================
        // POST: api/roles/{roleId}/permissions
        // =========================
        [HttpPost("{roleId}/permissions")]
        [Authorize(Policy = "ROLES_PERMISSIONS_ASSIGN")]
        public IActionResult AssignPermissionToRole(
            int roleId,
            AssignPermissionDto dto)
        {
            var role = _context.Roles.FirstOrDefault(r => r.Id == roleId);
            if (role == null)
                return NotFound("Role not found");

            var permission = _context.Permissions
                .FirstOrDefault(p => p.Code == dto.PermissionCode);

            if (permission == null)
                return BadRequest("Invalid permission code");

            var exists = _context.RolePermissions.Any(rp =>
                rp.Role == role.Name &&
                rp.PermissionId == permission.Id);

            if (exists)
                return BadRequest("Permission already assigned");

            _context.RolePermissions.Add(new RolePermission
            {
                Role = role.Name,
                PermissionId = permission.Id
            });

            _context.SaveChanges();

            return Ok("Permission assigned successfully");
        }

        // =========================
        // DELETE: api/roles/{roleId}/permissions/{permissionCode}
        // =========================
        [HttpDelete("{roleId}/permissions/{permissionCode}")]
        [Authorize(Policy = "ROLES_PERMISSIONS_REMOVE")]
        public IActionResult RemovePermissionFromRole(
            int roleId,
            string permissionCode)
        {
            var role = _context.Roles.FirstOrDefault(r => r.Id == roleId);
            if (role == null)
                return NotFound("Role not found");

            var permission = _context.Permissions
                .FirstOrDefault(p => p.Code == permissionCode);

            if (permission == null)
                return NotFound("Permission not found");

            var rolePermission = _context.RolePermissions.FirstOrDefault(rp =>
                rp.Role == role.Name &&
                rp.PermissionId == permission.Id);

            if (rolePermission == null)
                return NotFound("Permission not assigned to role");

            _context.RolePermissions.Remove(rolePermission);
            _context.SaveChanges();

            return Ok("Permission removed successfully");
        }
        // =========================
        // POST: api/roles
        // =========================
        [HttpPost]
        [Authorize(Policy = "ROLES_CREATE")]
        public IActionResult CreateRole(CreateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Role name is required");

            var roleName = dto.Name.Trim();

            var exists = _context.Roles.Any(r => r.Name == roleName);
            if (exists)
                return BadRequest("Role already exists");

            var role = new Role
            {
                Name = roleName
            };

            _context.Roles.Add(role);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Role created successfully",
                roleId = role.Id
            });
        }
    }
}