using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using SchoolSystemAPI.Models.DTOs;

namespace SchoolSystemAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "TarbeyaGeneralAdmin,TarbeyaFamilyAdmin,TarbeyaServant,Admin")]
    public class TarbeyaMahraganController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TarbeyaMahraganController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserRole() => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

        // ==================== EVENTS & COMPETITIONS (Admins Only) ====================

        [HttpGet("events")]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.MahraganEvents.Include(e => e.Competitions).ToListAsync();
            return Ok(new { success = true, events });
        }

        [HttpPost("events")]
        public async Task<IActionResult> CreateEvent([FromBody] MahraganEvent ev)
        {
            if (GetCurrentUserRole() == "TarbeyaServant") return Forbid();
            _context.MahraganEvents.Add(ev);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, ev });
        }

        [HttpPost("events/{eventId}/competitions")]
        public async Task<IActionResult> CreateCompetition(int eventId, [FromBody] MahraganCompetition comp)
        {
            if (GetCurrentUserRole() == "TarbeyaServant") return Forbid();
            comp.EventId = eventId;
            _context.MahraganCompetitions.Add(comp);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, comp });
        }

        // ==================== ENROLLMENTS (Servants +) ====================

        [HttpGet("enrollments")]
        public async Task<IActionResult> GetEnrollments([FromQuery] int competitionId)
        {
            var enrollments = await _context.MahraganEnrollments
                .Include(e => e.Student)
                .Include(e => e.Scores)
                .Where(e => e.CompetitionId == competitionId)
                .Select(e => new {
                    e.Id,
                    e.BarcodeString,
                    StudentName = e.Student.Name,
                    Scores = e.Scores.Select(s => new { s.StageName, s.Score, s.IsQualified })
                })
                .ToListAsync();
            
            return Ok(new { success = true, enrollments });
        }

        [HttpPost("enroll")]
        public async Task<IActionResult> Enroll([FromBody] MahraganEnrollment enrollment)
        {
            // Generate a unique barcode if not provided
            if (string.IsNullOrEmpty(enrollment.BarcodeString))
            {
                enrollment.BarcodeString = "MHR-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            }

            // Check if already enrolled
            bool exists = await _context.MahraganEnrollments.AnyAsync(e => 
                e.StudentId == enrollment.StudentId && e.CompetitionId == enrollment.CompetitionId);
            
            if (exists) return BadRequest(new { success = false, message = "Student already enrolled in this competition." });

            _context.MahraganEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, barcode = enrollment.BarcodeString });
        }

        // ==================== SCORES ====================

        [HttpPost("scores")]
        public async Task<IActionResult> RecordScore([FromBody] RecordMahraganScoreDto dto)
        {
            var enrollment = await _context.MahraganEnrollments
                .Include(e => e.Competition)
                .FirstOrDefaultAsync(e => e.Id == dto.EnrollmentId);

            if (enrollment == null) return NotFound("Enrollment not found");

            // Calculate IsQualified based on PassingScore
            bool isQualified = dto.Score >= enrollment.Competition.PassingScore;

            var score = new MahraganScore
            {
                EnrollmentId = dto.EnrollmentId,
                StageName = dto.StageName,
                Score = dto.Score,
                IsQualified = isQualified
            };

            _context.MahraganScores.Add(score);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, isQualified });
        }
    }
}
