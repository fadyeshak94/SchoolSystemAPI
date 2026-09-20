using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using System.Text.Json;

namespace SchoolSystemAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MothersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MothersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> AddMother([FromBody] AddMotherRequest request)
    {
        if (!HasSmartMotherAccess()) return Forbid();

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Phone))
        {
            return BadRequest(new { success = false, message = "الاسم ورقم الهاتف مطلوبان" });
        }

        var mother = new Mother
        {
            Name = request.Name,
            Phone = request.Phone,
            Whatsapp = request.Whatsapp ?? "",
            DateOfBirth = request.DateOfBirth,
            HusbandName = request.HusbandName ?? "",
            Occupation = request.Occupation ?? "",
            ConfessionFather = request.ConfessionFather ?? ""
        };

        _context.Mothers.Add(mother);
        await _context.SaveChangesAsync();

        if (request.StudentIds != null && request.StudentIds.Any())
        {
            var students = await _context.Students
                .Where(s => request.StudentIds.Contains(s.Id))
                .ToListAsync();

            // Auto-link siblings sharing the same FamilyId
            var familyIds = students.Where(s => !string.IsNullOrEmpty(s.FamilyId)).Select(s => s.FamilyId).Distinct().ToList();
            if (familyIds.Any())
            {
                var siblings = await _context.Students
                    .Where(s => familyIds.Contains(s.FamilyId))
                    .ToListAsync();
                
                students = students.Union(siblings).DistinctBy(s => s.Id).ToList();
            }

            foreach (var student in students)
            {
                student.MotherId = mother.Id;
            }
            await _context.SaveChangesAsync();
        }

        return Ok(new { success = true, message = "تم تسجيل الأم بنجاح" });
    }

    [HttpGet]
    public async Task<IActionResult> GetMothers()
    {
        if (!HasSmartMotherAccess()) return Forbid();

        var mothers = await _context.Mothers
            .Include(m => m.Children)
                .ThenInclude(c => c.Family)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Phone,
                m.Whatsapp,
                m.DateOfBirth,
                m.HusbandName,
                m.Occupation,
                m.ConfessionFather,
                ChildrenNames = string.Join("، ", m.Children.Select(c => c.Name)),
                FamilyAddress = m.Children.Where(c => c.Family != null).Select(c => c.Family.Address).FirstOrDefault() ?? "",
                FatherPhone = m.Children.Where(c => c.Family != null).Select(c => c.Family.FatherPhone).FirstOrDefault() ?? ""
            })
            .ToListAsync();

        return Ok(new { success = true, mothers });
    }

    [HttpGet("search-students")]
    public async Task<IActionResult> SearchStudents([FromQuery] string query)
    {
        if (!HasSmartMotherAccess()) return Forbid();

        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(new { success = true, students = new List<object>() });
        }

        var students = await _context.Students
            .Where(s => s.Name.Contains(query))
            .Take(10)
            .Select(s => new { s.Id, s.Name, s.GovGrade })
            .ToListAsync();

        return Ok(new { success = true, students });
    }

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance([FromQuery] DateTime date)
    {
        if (!HasSmartMotherAccess()) return Forbid();

        var mothers = await _context.Mothers.ToListAsync();
        
        var dateOnly = date.Date;
        var attendances = await _context.MotherAttendances
            .Where(a => a.Date.Date == dateOnly)
            .ToListAsync();

        var result = mothers.Select(m => new
        {
            m.Id,
            m.Name,
            m.Phone,
            m.Whatsapp,
            Status = attendances.FirstOrDefault(a => a.MotherId == m.Id)?.Status ?? ""
        });

        return Ok(new { success = true, attendance = result });
    }

    [HttpPost("attendance")]
    public async Task<IActionResult> SaveAttendance([FromBody] SaveMotherAttendanceRequest request)
    {
        if (!HasSmartMotherAccess()) return Forbid();

        var dateOnly = request.Date.Date;
        
        var existing = await _context.MotherAttendances
            .Where(a => a.Date.Date == dateOnly)
            .ToListAsync();

        _context.MotherAttendances.RemoveRange(existing);

        var toAdd = request.Attendances.Select(a => new MotherAttendance
        {
            MotherId = a.MotherId,
            Date = dateOnly,
            Status = a.Status
        });

        _context.MotherAttendances.AddRange(toAdd);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "تم حفظ الحضور بنجاح" });
    }

    private bool HasSmartMotherAccess()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.ToLower();
        if (role == "admin" || role == "headsecretary") return true;
        
        var canAccess = User.FindFirst("CanAccessSmartMother")?.Value;
        return canAccess == "True";
    }
}

public class AddMotherRequest
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Whatsapp { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? HusbandName { get; set; }
    public string? Occupation { get; set; }
    public string? ConfessionFather { get; set; }
    public List<int>? StudentIds { get; set; }
}

public class SaveMotherAttendanceRequest
{
    public DateTime Date { get; set; }
    public List<MotherAttendanceDto> Attendances { get; set; } = new();
}

public class MotherAttendanceDto
{
    public int MotherId { get; set; }
    public string Status { get; set; } = string.Empty;
}
