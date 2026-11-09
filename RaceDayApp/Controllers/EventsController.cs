using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApp.Data;
using RaceDayApp.DTOs;
using RaceDayApp.Models;
using System.Security.Claims;

namespace RaceDayApp.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public EventsController(RaceDayDbContext context) => _context = context;

        private int? CurrentUserId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : (int?)null;
        }

        private static string? Validate(EventCreateUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EventName)) return "Event name is required.";
            if (string.IsNullOrWhiteSpace(dto.Location)) return "Location is required.";
            if (string.IsNullOrWhiteSpace(dto.Distance)) return "Distance is required.";
            if (string.IsNullOrWhiteSpace(dto.EventType)) return "Event type is required.";
            return null;
        }

        // Public: all events (projection so the organiser's password hash is never returned)
        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventID,
                    e.EventName,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.Distance,
                    e.EventType,
                    e.OrganizerID,
                    OrganizerName = e.Organizer.FirstName + " " + e.Organizer.LastName
                })
                .ToListAsync();
            return Ok(events);
        }

        // Organiser: only the events they created
        [HttpGet("mine")]
        [Authorize(Policy = "OrganiserOnly")]
        public async Task<IActionResult> GetMyEvents()
        {
            var organizerId = CurrentUserId();
            if (organizerId == null) return Unauthorized();

            var events = await _context.Events
                .Where(e => e.OrganizerID == organizerId)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventID,
                    e.EventName,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.Distance,
                    e.EventType
                })
                .ToListAsync();
            return Ok(events);
        }

        [HttpPost]
        [Authorize(Policy = "OrganiserOnly")]
        public async Task<IActionResult> CreateEvent([FromBody] EventCreateUpdateDto dto)
        {
            var organizerId = CurrentUserId();
            if (organizerId == null) return Unauthorized();

            var error = Validate(dto);
            if (error != null) return BadRequest(new { message = error });

            var ev = new Event
            {
                OrganizerID = organizerId.Value,
                EventName = dto.EventName,
                Description = dto.Description ?? string.Empty,
                EventDate = dto.EventDate,
                Location = dto.Location,
                Distance = dto.Distance,
                EventType = dto.EventType
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();
            return StatusCode(201, new { message = "Event created", eventId = ev.EventID });
        }

        // Organiser: update their own event
        [HttpPut("{id:int}")]
        [Authorize(Policy = "OrganiserOnly")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventCreateUpdateDto dto)
        {
            var organizerId = CurrentUserId();
            if (organizerId == null) return Unauthorized();

            var error = Validate(dto);
            if (error != null) return BadRequest(new { message = error });

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound(new { message = "Event not found." });
            if (ev.OrganizerID != organizerId) return Forbid();

            ev.EventName = dto.EventName;
            ev.Description = dto.Description ?? string.Empty;
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location;
            ev.Distance = dto.Distance;
            ev.EventType = dto.EventType;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Event updated" });
        }

        // Organiser: delete their own event (and its enrolments + results)
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "OrganiserOnly")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var organizerId = CurrentUserId();
            if (organizerId == null) return Unauthorized();

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound(new { message = "Event not found." });
            if (ev.OrganizerID != organizerId) return Forbid();

            var enrollments = await _context.Enrollments.Where(e => e.EventID == id).ToListAsync();
            var enrollmentIds = enrollments.Select(e => e.EnrollmentID).ToList();
            var results = await _context.Results.Where(r => enrollmentIds.Contains(r.EnrollmentID)).ToListAsync();

            // SaveChanges runs as a single transaction
            _context.Results.RemoveRange(results);
            _context.Enrollments.RemoveRange(enrollments);
            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Event deleted" });
        }

        // Organiser: participants enrolled in their event, with any result already captured
        [HttpGet("{id:int}/enrollments")]
        [Authorize(Policy = "OrganiserOnly")]
        public async Task<IActionResult> GetEventEnrollments(int id)
        {
            var organizerId = CurrentUserId();
            if (organizerId == null) return Unauthorized();

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound(new { message = "Event not found." });
            if (ev.OrganizerID != organizerId) return Forbid();

            var enrollments = await _context.Enrollments
                .Where(e => e.EventID == id)
                .Select(e => new
                {
                    e.EnrollmentID,
                    ParticipantName = e.Participant.FirstName + " " + e.Participant.LastName,
                    CategoryName = e.Category.CategoryName
                })
                .ToListAsync();

            var ids = enrollments.Select(e => e.EnrollmentID).ToList();
            var results = await _context.Results
                .Where(r => ids.Contains(r.EnrollmentID))
                .ToListAsync();

            var data = enrollments.Select(e =>
            {
                var r = results.FirstOrDefault(x => x.EnrollmentID == e.EnrollmentID);
                return new
                {
                    e.EnrollmentID,
                    e.ParticipantName,
                    e.CategoryName,
                    Result = r == null ? null : new
                    {
                        r.ResultID,
                        FinishTime = ResultsController.FormatTime(r.FinishTime),
                        r.FinishingPosition
                    }
                };
            });

            return Ok(data);
        }
    }
}