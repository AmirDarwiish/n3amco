using CourseCenter.Api;
using CourseCenter.Api.Leads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CourseCenter.Api.Controllers
{
    [ApiController]
    [Route("api/mentions")]
    [Authorize]
    public class MentionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MentionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /api/mentions/mine
        [HttpGet("mine")]
        public IActionResult GetMyMentions()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var mentions = _context.LeadMentions
                .Where(m => m.MentionedUserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new
                {
                    MentionId = m.Id,
                    LeadId = m.LeadNote.LeadId,
                    LeadNoteId = m.LeadNoteId,
                    m.CreatedAt,
                    m.IsRead
                })
                .ToList();

            return Ok(mentions);
        }

        // POST: /api/mentions/{id}/read
        [HttpPost("{id:int}/read")]
        public IActionResult MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var mention = _context.LeadMentions
                .FirstOrDefault(m => m.Id == id && m.MentionedUserId == userId);

            if (mention == null)
                return NotFound();

            if (!mention.IsRead)
            {
                mention.IsRead = true;
                _context.SaveChanges();
            }

            return Ok();
        }

        // GET: /api/mentions/unread-count
        [HttpGet("unread-count")]
        public IActionResult GetUnreadCount()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var count = _context.LeadMentions
                .Count(m => m.MentionedUserId == userId && !m.IsRead);

            return Ok(new { unreadCount = count });
        }
    }
}
