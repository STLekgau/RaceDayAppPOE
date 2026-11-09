using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApp.Data;
using RaceDayApp.DTOs;
using RaceDayApp.Models;
using System.Security.Claims;

namespace RaceDayApp.Controllers
{
    [Route("api/results")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public ResultsController(RaceDayDbContext context) => _context = context;

        public static string FormatTime(TimeSpan t) =>
            $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";

        private int? CurrentUserId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : (int?)null;
        }

        private static string? Validate(ResultCreateUpdateDto dto, out TimeSpan time)
        {
            time = default;
            if (!TimeSpan.TryParse(dto.FinishTime, out time) || time <= TimeSpan.Zero)
                return "Finish time must be in the format hh:mm:ss.";
            if (dto.FinishingPosition < 1)
                return "Finishing position must be 1 or higher.";
            return null;
        }

        // Organiser: publish a result for an enrolment in one of THEIR events
        [HttpPost]
        [Authorize(Policy = "OrganiserOnly")]
        public async Task<IActionResult> AddResult([FromBody] ResultCreateUpdateDto dto)
        {
            var organizerId = CurrentUserId();
            if (organizerId == null) return Unauthorized();

            var error = Validate(dto, out var time);
            if (error != null) return BadRequest(new { message = error });

            var enrollment = await _context.Enrollments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(e => e.EnrollmentID == dto.EnrollmentID);
            if (enrollment == null) return NotFound(new { message = "Enrolment not found." });
            if (enrollment.Event.OrganizerID != organizerId) return Forbid();

            if (await _context.Results.AnyAsync(r => r.EnrollmentID == dto.EnrollmentID))
                return Conflict(new { message = "A result already exists for this participant. Update it instead." });

            _context.Results.Add(new Result
            {
                EnrollmentID = dto.EnrollmentID,
                FinishTime = time,
                FinishingPosition = dto.FinishingPosition
            });
            await _context.SaveChangesAsync();
            return StatusCode(201, new { message = "Result published" });
        }

        // Organiser: correct a result they published
        [HttpPut("{id:int}")]
        [Authorize(Policy = "OrganiserOnly")]
        public async Task<IActionResult> UpdateResult(int id, [FromBody] ResultCreateUpdateDto dto)
        {
            var organizerId = CurrentUserId();
            if (organizerId == null) return Unauthorized();

            var error = Validate(dto, out var time);
            if (error != null) return BadRequest(new { message = error });

            var result = await _context.Results
                .Include(r => r.Enrollment).ThenInclude(e => e.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);
            if (result == null) return NotFound(new { message = "Result not found." });
            if (result.Enrollment.Event.OrganizerID != organizerId) return Forbid();

            result.FinishTime = time;
            result.FinishingPosition = dto.FinishingPosition;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Result updated" });
        }

        // Participant: their own results history
        [HttpGet("my")]
        [Authorize(Policy = "ParticipantOnly")]
        public async Task<IActionResult> GetMyResults()
        {
            var participantId = CurrentUserId();
            if (participantId == null) return Unauthorized();

            var rows = await _context.Results
                .Where(r => r.Enrollment.ParticipantID == participantId)
                .Select(r => new
                {
                    r.ResultID,
                    EventName = r.Enrollment.Event.EventName,
                    EventDate = r.Enrollment.Event.EventDate,
                    Location = r.Enrollment.Event.Location,
                    Distance = r.Enrollment.Event.Distance,
                    CategoryName = r.Enrollment.Category.CategoryName,
                    r.FinishTime,
                    r.FinishingPosition
                })
                .OrderByDescending(r => r.EventDate)
                .ToListAsync();

            return Ok(rows.Select(r => new
            {
                r.ResultID,
                r.EventName,
                r.EventDate,
                r.Location,
                r.Distance,
                r.CategoryName,
                FinishTime = FormatTime(r.FinishTime),
                r.FinishingPosition
            }));
        }
    }
}