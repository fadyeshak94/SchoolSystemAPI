using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using System.Security.Claims;
using MediatR;
using SchoolSystemAPI.Features.Notifications;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TarbeyaStudentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMediator _mediator;

    public TarbeyaStudentsController(ApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    private async Task<AppUser?> GetCurrentUserAsync()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username)) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    [HttpGet]
    public async Task<IActionResult> GetStudents([FromQuery] int? classId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var query = _context.TarbeyaStudents
            .Include(s => s.Class)
            .ThenInclude(c => c!.Stage)
            .Include(s => s.AreaNavigation)
            .AsQueryable();

        // Role based filtering
        if (user.Role == "TarbeyaServant")
        {
            if (user.TarbeyaClassId == null) return Ok(new { success = true, students = new List<object>() });
            query = query.Where(s => s.ClassId == user.TarbeyaClassId);
        }
        else if (user.Role == "TarbeyaFamilyAdmin")
        {
            if (user.TarbeyaFamilyId == null) return Ok(new { success = true, students = new List<object>() });
            query = query.Where(s => s.Class!.Stage!.FamilyId == user.TarbeyaFamilyId);
            if (classId.HasValue)
            {
                query = query.Where(s => s.ClassId == classId.Value);
            }
        }
        else if (user.Role == "TarbeyaGeneralAdmin" || user.Role == "Admin")
        {
            if (classId.HasValue)
            {
                query = query.Where(s => s.ClassId == classId.Value);
            }
        }
        else
        {
            return Forbid();
        }

        var students = await query.ToListAsync();
        
        var result = students.Select(s => new {
            s.Id,
            s.Name,
            s.BirthDate,
            s.Phone,
            s.ParentPhone,
            s.Address,
            AreaId = s.AreaId,
            AreaName = s.AreaNavigation != null ? s.AreaNavigation.Name : "",
            s.GeneralNotes,
            s.MedicalNotes,
            s.Barcode,
            s.TotalPoints,
            // Only expose PrivateNotes if they have access
            PrivateNotes = s.PrivateNotes,
            ClassId = s.ClassId,
            ClassName = s.Class?.Name,
            StageName = s.Class?.Stage?.Name
        });

        return Ok(new { success = true, students = result });
    }

    [HttpPost]
    public async Task<IActionResult> AddStudent([FromBody] TarbeyaStudent dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        // Permissions check for adding
        if (user.Role == "TarbeyaServant" && dto.ClassId != user.TarbeyaClassId)
        {
            return Forbid();
        }

        if (user.Role == "TarbeyaFamilyAdmin")
        {
            var targetClass = await _context.TarbeyaClasses.Include(c => c.Stage).FirstOrDefaultAsync(c => c.Id == dto.ClassId);
            if (targetClass?.Stage?.FamilyId != user.TarbeyaFamilyId)
                return Forbid();
        }

        _context.TarbeyaStudents.Add(dto);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, student = dto });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] TarbeyaStudent dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var student = await _context.TarbeyaStudents.Include(s => s.Class).ThenInclude(c => c!.Stage).FirstOrDefaultAsync(s => s.Id == id);
        if (student == null) return NotFound("Student not found");

        // Validate permissions
        if (user.Role == "TarbeyaServant" && student.ClassId != user.TarbeyaClassId)
            return Forbid();
            
        if (user.Role == "TarbeyaFamilyAdmin" && student.Class?.Stage?.FamilyId != user.TarbeyaFamilyId)
            return Forbid();

        student.Name = dto.Name;
        student.BirthDate = dto.BirthDate;
        student.Phone = dto.Phone;
        student.ParentPhone = dto.ParentPhone;
        student.Address = dto.Address;
        student.AreaId = dto.AreaId;
        student.ConfessionFather = dto.ConfessionFather;
        student.GeneralNotes = dto.GeneralNotes;
        student.PrivateNotes = dto.PrivateNotes;
        student.MedicalNotes = dto.MedicalNotes;
        student.Barcode = dto.Barcode;
        
        // Changing class is allowed for FamilyAdmin inside same family or GenAdmin/Admin
        if (student.ClassId != dto.ClassId)
        {
            if (user.Role == "TarbeyaServant") return Forbid("Cannot change class");
            if (user.Role == "TarbeyaFamilyAdmin")
            {
                var targetClass = await _context.TarbeyaClasses.Include(c => c.Stage).FirstOrDefaultAsync(c => c.Id == dto.ClassId);
                if (targetClass?.Stage?.FamilyId != user.TarbeyaFamilyId) return Forbid();
            }
            student.ClassId = dto.ClassId;
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Student updated successfully" });
    }

    // Spiritual Tracking Endpoints
    [HttpPost("{id}/confession")]
    public async Task<IActionResult> UpdateConfession(int id, [FromBody] UpdateConfessionDto dto)
    {
        var result = await _mediator.Send(new UpdateConfessionCommand
        {
            StudentId = id,
            ConfessionDate = dto.ConfessionDate,
            FatherConfessorName = dto.FatherConfessorName
        });
        if (!result) return NotFound();
        return Ok(new { success = true });
    }

    [HttpPost("{id}/liturgy-attendance")]
    public async Task<IActionResult> RecordLiturgyAttendance(int id, [FromBody] LiturgyAttendanceDto dto)
    {
        var result = await _mediator.Send(new RecordLiturgyAttendanceCommand
        {
            StudentId = id,
            Date = dto.Date,
            Status = dto.Status
        });
        if (!result) return NotFound();
        return Ok(new { success = true });
    }
}

public class UpdateConfessionDto
{
    public DateTime ConfessionDate { get; set; }
    public string? FatherConfessorName { get; set; }
}

public class LiturgyAttendanceDto
{
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Attended";
}
