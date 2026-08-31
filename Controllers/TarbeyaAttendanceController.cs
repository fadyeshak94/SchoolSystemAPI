using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using System.Security.Claims;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TarbeyaAttendanceController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TarbeyaAttendanceController(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<AppUser?> GetCurrentUserAsync()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username)) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    [HttpPost]
    public async Task<IActionResult> RecordAttendance([FromBody] TarbeyaAttendance dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var student = await _context.TarbeyaStudents.Include(s => s.Class).ThenInclude(c => c!.Stage).FirstOrDefaultAsync(s => s.Id == dto.StudentId);
        if (student == null) return NotFound("Student not found");

        if (user.Role == "TarbeyaServant" && student.ClassId != user.TarbeyaClassId)
            return Forbid();
            
        if (user.Role == "TarbeyaFamilyAdmin" && student.Class?.Stage?.FamilyId != user.TarbeyaFamilyId)
            return Forbid();

        var date = dto.Date.Date; // strip time

        var existing = await _context.TarbeyaAttendances.FirstOrDefaultAsync(a => a.StudentId == dto.StudentId && a.Date == date);
        if (existing != null)
        {
            existing.Status = dto.Status;
        }
        else
        {
            var att = new TarbeyaAttendance
            {
                StudentId = dto.StudentId,
                Date = date,
                Status = dto.Status
            };
            _context.TarbeyaAttendances.Add(att);
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Attendance recorded" });
    }

    [HttpGet("date/{date}")]
    public async Task<IActionResult> GetAttendanceByDate(DateTime date, [FromQuery] int? classId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var query = _context.TarbeyaAttendances
            .Include(a => a.Student)
            .ThenInclude(s => s!.Class)
            .ThenInclude(c => c!.Stage)
            .Where(a => a.Date == date.Date)
            .AsQueryable();

        // Apply Role filter
        if (user.Role == "TarbeyaServant")
        {
            query = query.Where(a => a.Student!.ClassId == user.TarbeyaClassId);
        }
        else if (user.Role == "TarbeyaFamilyAdmin")
        {
            query = query.Where(a => a.Student!.Class!.Stage!.FamilyId == user.TarbeyaFamilyId);
            if (classId.HasValue) query = query.Where(a => a.Student!.ClassId == classId.Value);
        }
        else if (user.Role == "TarbeyaGeneralAdmin" || user.Role == "Admin")
        {
            if (classId.HasValue) query = query.Where(a => a.Student!.ClassId == classId.Value);
        }
        else
        {
            return Forbid();
        }

        var records = await query.Select(a => new {
            a.Id,
            a.StudentId,
            StudentName = a.Student!.Name,
            a.Date,
            a.Status
        }).ToListAsync();

        return Ok(new { success = true, records });
    }
}
