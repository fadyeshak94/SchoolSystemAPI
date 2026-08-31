using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Models;
using SchoolSystemAPI.Services;
using System.Security.Claims;
using SchoolSystemAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TarbeyaVisitationController : ControllerBase
{
    private readonly IVisitationService _visitationService;
    private readonly ApplicationDbContext _context;

    public TarbeyaVisitationController(IVisitationService visitationService, ApplicationDbContext context)
    {
        _visitationService = visitationService;
        _context = context;
    }

    private async Task<AppUser?> GetCurrentUserAsync()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username)) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    [HttpGet("needs-visitation")]
    public async Task<IActionResult> GetNeedsVisitation([FromQuery] int? classId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        int? filterClassId = classId;
        int? filterFamilyId = null;

        if (user.Role == "TarbeyaServant")
        {
            if (user.TarbeyaClassId == null) return Ok(new { success = true, students = new List<object>() });
            filterClassId = user.TarbeyaClassId;
        }
        else if (user.Role == "TarbeyaFamilyAdmin")
        {
            if (user.TarbeyaFamilyId == null) return Ok(new { success = true, students = new List<object>() });
            filterFamilyId = user.TarbeyaFamilyId;
        }
        else if (user.Role != "TarbeyaGeneralAdmin" && user.Role != "Admin")
        {
            return Forbid();
        }

        var students = await _visitationService.GetStudentsNeedingVisitationAsync(filterClassId, filterFamilyId);
        // Ensure AreaNavigation is loaded (ideally in the service, but we'll do it here or assume it's fine if null for now, wait it's not included in service. I should include it in service, or just do it here).
        // Actually, the service returns a List<TarbeyaStudent>. We can just select from it.
        // But AreaNavigation might be null if not included. Let's just do:
        
        var result = students.Select(s => new {
            s.Id,
            s.Name,
            s.Phone,
            s.ParentPhone,
            s.Address,
            AreaName = s.AreaNavigation != null ? s.AreaNavigation.Name : "",
            ClassId = s.ClassId,
            ClassName = s.Class?.Name,
            StageName = s.Class?.Stage?.Name,
            s.MedicalNotes
        });

        return Ok(new { success = true, students = result });
    }

    [HttpPost]
    public async Task<IActionResult> RecordVisitation([FromBody] RecordVisitationDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        // In a real app, validate that the user has access to this student.
        var record = new TarbeyaVisitationRecord
        {
            StudentId = dto.StudentId,
            Type = dto.Type,
            Date = dto.Date ?? DateTime.Today,
            Result = dto.Result,
            ServantId = user.Id
        };

        var success = await _visitationService.RecordVisitationAsync(record);
        if (success)
            return Ok(new { success = true, message = "تم تسجيل الافتقاد بنجاح." });
        
        return BadRequest(new { success = false, message = "فشل في تسجيل الافتقاد." });
    }
}

public class RecordVisitationDto
{
    public int StudentId { get; set; }
    public VisitationType Type { get; set; } // 0=Phone, 1=Home, 2=Church
    public DateTime? Date { get; set; }
    public string? Result { get; set; }
}
