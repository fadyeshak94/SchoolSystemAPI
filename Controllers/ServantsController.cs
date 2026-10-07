using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ServantsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ServantsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("stages")]
    [Authorize(Roles = "Admin,HeadSecretary,StageSupervisor,Secretary")]
    public async Task<IActionResult> GetStages()
    {
        var stages = await _context.ClassRooms
            .Select(c => c.Stage)
            .Distinct()
            .ToListAsync();

        return Ok(new { success = true, data = stages });
    }

    [HttpGet("assignments")]
    [Authorize(Roles = "Admin,HeadSecretary,StageSupervisor,Secretary")]
    public async Task<IActionResult> GetServantsAssignments([FromQuery] string? academicYear)
    {
        var assignments = await _context.ServantAssignments
            .Include(sa => sa.User)
            .Include(sa => sa.ClassRoom)
            .Where(sa => string.IsNullOrEmpty(academicYear) || sa.AcademicYear == academicYear)
            .ToListAsync();

        var result = assignments.Select(sa => new
        {
            sa.Id,
            sa.UserId,
            ServantName = sa.User.NameAR ?? sa.User.Username,
            sa.ClassRoomId,
            ClassName = sa.ClassRoom.Name,
            Stage = sa.ClassRoom.Stage,
            sa.SubjectName
        }).ToList();

        return Ok(new { success = true, data = result });
    }

    [HttpGet("attendance")]
    [Authorize(Roles = "Admin,HeadSecretary,StageSupervisor,Secretary")]
    public async Task<IActionResult> GetAttendance([FromQuery] string date)
    {
        if (!DateTime.TryParse(date, out DateTime parsedDate))
            return BadRequest(new { success = false, message = "تاريخ غير صالح" });

        var records = await _context.ServantAttendances
            .Where(a => a.Date.Date == parsedDate.Date)
            .ToListAsync();

        return Ok(new { success = true, data = records });
    }

    [HttpPost("attendance")]
    [Authorize(Roles = "Admin,HeadSecretary,StageSupervisor,Secretary")]
    public async Task<IActionResult> SaveAttendance([FromBody] SaveServantAttendanceDto dto)
    {
        if (!DateTime.TryParse(dto.Date, out DateTime parsedDate))
            return BadRequest(new { success = false, message = "تاريخ غير صالح" });

        // Ensure it's Friday
        if (parsedDate.DayOfWeek != DayOfWeek.Friday)
            return BadRequest(new { success = false, message = "الغياب للخدام مسموح يوم الجمعة فقط" });

        var existingRecords = await _context.ServantAttendances
            .Where(a => a.Date.Date == parsedDate.Date)
            .ToListAsync();

        foreach (var req in dto.Records)
        {
            var existing = existingRecords.FirstOrDefault(r => r.ServantId == req.ServantId);
            if (existing != null)
            {
                existing.Status = req.Status;
                existing.IsExcused = req.IsExcused;
                existing.AcademicYear = dto.AcademicYear;
                _context.ServantAttendances.Update(existing);
            }
            else
            {
                await _context.ServantAttendances.AddAsync(new ServantAttendance
                {
                    Date = parsedDate.Date,
                    ServantId = req.ServantId,
                    Status = req.Status,
                    IsExcused = req.IsExcused,
                    AcademicYear = dto.AcademicYear
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "تم حفظ غياب الخدام بنجاح" });
    }

    [HttpGet("performance")]
    [Authorize(Roles = "Admin,HeadSecretary,StageSupervisor,Secretary")]
    public async Task<IActionResult> GetPerformance([FromQuery] string? academicYear)
    {
        var users = await _context.Users
            .Include(u => u.ServantAssignments).ThenInclude(sa => sa.ClassRoom)
            .Where(u => u.ServantAssignments.Any())
            .ToListAsync();

        var attendances = await _context.ServantAttendances
            .Where(a => string.IsNullOrEmpty(academicYear) || a.AcademicYear == academicYear)
            .ToListAsync();

        var result = new List<object>();

        foreach (var u in users)
        {
            var stRecords = attendances.Where(r => r.ServantId == u.Id).ToList();
            int totalDays = stRecords.Count;
            int presentDays = stRecords.Count(r => r.Status == "Present");
            int absentDays = stRecords.Count(r => r.Status == "Absent" && !r.IsExcused);
            int excusedDays = stRecords.Count(r => r.IsExcused);
            
            decimal absencePercentage = totalDays > 0 ? (decimal)absentDays / totalDays * 100m : 0;
            
            var classesAssigned = string.Join("، ", u.ServantAssignments.Select(sa => $"{sa.SubjectName} ({sa.ClassRoom.Name})"));

            result.Add(new
            {
                ServantId = u.Id,
                ServantName = u.NameAR ?? u.Username,
                Title = u.Title ?? "خادم",
                Classes = classesAssigned,
                TotalDays = totalDays,
                PresentDays = presentDays,
                AbsentDays = absentDays,
                ExcusedDays = excusedDays,
                AbsencePercentage = Math.Round(absencePercentage, 1)
            });
        }

        return Ok(new { success = true, data = result.OrderBy(r => ((dynamic)r).ServantName) });
    }
}

public class SaveServantAttendanceDto
{
    public string Date { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public List<ServantAttendanceRecordDto> Records { get; set; } = new();
}

public class ServantAttendanceRecordDto
{
    public int ServantId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsExcused { get; set; }
}
