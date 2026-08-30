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
public class TarbeyaDashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TarbeyaDashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<AppUser?> GetCurrentUserAsync()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username)) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    [HttpGet("followup")]
    public async Task<IActionResult> GetFollowUpStudents([FromQuery] string? area)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var query = _context.TarbeyaStudents
            .Include(s => s.Class)
            .ThenInclude(c => c!.Stage)
            .AsQueryable();

        // Apply Role-based filtering
        if (user.Role == "TarbeyaServant")
        {
            if (user.TarbeyaClassId == null) return Ok(new { success = true, students = new List<object>() });
            query = query.Where(s => s.ClassId == user.TarbeyaClassId);
        }
        else if (user.Role == "TarbeyaFamilyAdmin")
        {
            if (user.TarbeyaFamilyId == null) return Ok(new { success = true, students = new List<object>() });
            query = query.Where(s => s.Class!.Stage!.FamilyId == user.TarbeyaFamilyId);
        }
        else if (user.Role == "TarbeyaGeneralAdmin")
        {
            // Full access
        }
        else
        {
            return Forbid();
        }

        // Apply Area filter if provided
        if (!string.IsNullOrEmpty(area))
        {
            query = query.Where(s => s.Area == area);
        }

        var students = await query.ToListAsync();

        var followUpList = new List<object>();

        // Find students absent for the last 3 weeks
        var today = DateTime.Today;
        var threeWeeksAgo = today.AddDays(-21);

        foreach (var student in students)
        {
            // Get attendances in the last 3 weeks
            var recentAttendances = await _context.TarbeyaAttendances
                .Where(a => a.StudentId == student.Id && a.Date >= threeWeeksAgo && a.Date <= today)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            // If there are at least 3 records and all 3 most recent are absent
            if (recentAttendances.Count >= 3)
            {
                var lastThree = recentAttendances.Take(3).ToList();
                if (lastThree.All(a => a.Status == TarbeyaAttendanceStatus.Absent))
                {
                    followUpList.Add(new
                    {
                        student.Id,
                        student.Name,
                        student.Phone,
                        student.Area,
                        ClassName = student.Class?.Name,
                        StageName = student.Class?.Stage?.Name,
                        GeneralNotes = student.GeneralNotes
                    });
                }
            }
            else if (recentAttendances.Count == 0) // No attendance recorded at all in 3 weeks, likely absent
            {
                followUpList.Add(new
                {
                    student.Id,
                    student.Name,
                    student.Phone,
                    student.Area,
                    ClassName = student.Class?.Name,
                    StageName = student.Class?.Stage?.Name,
                    GeneralNotes = student.GeneralNotes
                });
            }
        }

        return Ok(new { success = true, students = followUpList });
    }

    [HttpGet("birthdays")]
    public async Task<IActionResult> GetBirthdays([FromQuery] string filter = "month") // "week" or "month"
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var query = _context.TarbeyaStudents
            .Include(s => s.Class)
            .ThenInclude(c => c!.Stage)
            .Where(s => s.BirthDate != null)
            .AsQueryable();

        // Apply Role-based filtering
        if (user.Role == "TarbeyaServant")
        {
            if (user.TarbeyaClassId == null) return Ok(new { success = true, students = new List<object>() });
            query = query.Where(s => s.ClassId == user.TarbeyaClassId);
        }
        else if (user.Role == "TarbeyaFamilyAdmin")
        {
            if (user.TarbeyaFamilyId == null) return Ok(new { success = true, students = new List<object>() });
            query = query.Where(s => s.Class!.Stage!.FamilyId == user.TarbeyaFamilyId);
        }
        else if (user.Role == "TarbeyaGeneralAdmin")
        {
            // Full access
        }
        else
        {
            return Forbid();
        }

        var students = await query.ToListAsync();
        var today = DateTime.Today;

        var birthdayList = students.Where(s =>
        {
            var bday = s.BirthDate!.Value;
            var nextBday = new DateTime(today.Year, bday.Month, bday.Day);
            
            // Adjust if birthday already passed this year
            if (nextBday < today.AddDays(-7)) 
                nextBday = nextBday.AddYears(1);

            if (filter == "week")
            {
                var diff = (nextBday - today).TotalDays;
                return diff >= 0 && diff <= 7;
            }
            else // month
            {
                return nextBday.Month == today.Month;
            }
        }).Select(s => new
        {
            s.Id,
            s.Name,
            s.BirthDate,
            Age = today.Year - s.BirthDate!.Value.Year,
            s.Phone,
            ClassName = s.Class?.Name
        }).ToList();

        return Ok(new { success = true, students = birthdayList });
    }
}
