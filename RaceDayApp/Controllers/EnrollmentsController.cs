using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApp.Data;
using RaceDayApp.Models;
using System.Security.Claims;

namespace RaceDayApp.Controllers
{
    [Route("api/enrollments")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public EnrollmentsController(RaceDayDbContext context) => _context = context;

        [HttpPost]
        [Authorize(Policy = "ParticipantOnly")]
        public async Task<IActionResult> Enroll([FromBody] EnrollmentDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int participantId)) return Unauthorized();

            var enrollment = new Enrollment
            {
                ParticipantID = participantId,
                EventID = dto.EventID,
                CategoryID = dto.CategoryID
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();
            return StatusCode(201, new { message = "Enrolled successfully", enrollmentId = enrollment.EnrollmentID });
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyEnrollments()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int participantId)) return Unauthorized();

            var enrollments = await _context.Enrollments
                .Include(e => e.Event)
                .Include(e => e.Category)
                .Where(e => e.ParticipantID == participantId)
                .ToListAsync();

            return Ok(enrollments);
        }
    }

    public class EnrollmentDto
    {
        public int EventID { get; set; }
        public int CategoryID { get; set; }
    }
}