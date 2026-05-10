using DairySystem.Api.Users.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DairySystem.Api.Controllers
{
    [ApiController]
    [Route("api/permissions")]
    public class PermissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PermissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET: api/permissions
        // =========================
        [HttpGet]
        [Authorize(Policy = "PERMISSIONS_VIEW")]
        public IActionResult GetAll()
        {
            var permissions = _context.Permissions
                .Select(p => new
                {
                    p.Id,
                    p.Code,
                    p.Name,
                    p.Module
                })
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Name)
                .ToList();

            return Ok(permissions);
        }
    }
}   