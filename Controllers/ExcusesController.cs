using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ExcusesController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public ExcusesController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpPost]
    public async Task<IActionResult> CreateExcuse([FromBody] CreateExcuseDto request)
    {
        // يجب أن يكون التاريخ يوم جمعة
        if (request.Date.DayOfWeek != DayOfWeek.Friday)
        {
            return BadRequest(new { success = false, message = "العذر يجب أن يكون لتاريخ يوم جمعة فقط." });
        }

        var currentTerm = request.Term;
        var currentYear = request.AcademicYear;

        // التحقق من عدد الأعذار في التيرم
        var previousExcuses = await _uow.Excuses.FindAsync(e => 
            e.StudentId == request.StudentId && 
            e.Term == currentTerm && 
            e.AcademicYear == currentYear &&
            e.Status != "Rejected");

        if (previousExcuses.Count() >= 2)
        {
            return BadRequest(new { success = false, message = "لقد استنفذ الطالب الحد الأقصى للأعذار (2) في هذا التيرم." });
        }

        var excuse = new Excuse
        {
            StudentId = request.StudentId,
            Date = request.Date,
            Reason = request.Reason,
            Status = "Pending",
            Term = currentTerm,
            AcademicYear = currentYear
        };

        await _uow.Excuses.AddAsync(excuse);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم تقديم العذر بنجاح وهو قيد المراجعة." });
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetStudentExcuses(int studentId, [FromQuery] string term, [FromQuery] string year)
    {
        var excuses = await _uow.Excuses.FindAsync(e => 
            e.StudentId == studentId && 
            e.Term == term && 
            e.AcademicYear == year);

        var result = excuses.OrderByDescending(e => e.Date).Select(e => new {
            id = e.Id,
            date = e.Date.ToString("yyyy-MM-dd"),
            reason = e.Reason,
            status = e.Status
        });

        return Ok(new { success = true, excuses = result });
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Secretary,StageSupervisor")]
    public async Task<IActionResult> GetPendingExcuses()
    {
        var excuses = await _uow.Excuses.FindAsync(e => e.Status == "Pending");
        var studentIds = excuses.Select(e => e.StudentId).Distinct();
        var students = await _uow.Students.FindAsync(s => studentIds.Contains(s.Id));

        var result = excuses.Select(e => new {
            id = e.Id,
            studentId = e.StudentId,
            studentName = students.FirstOrDefault(s => s.Id == e.StudentId)?.Name ?? "غير معروف",
            date = e.Date.ToString("yyyy-MM-dd"),
            reason = e.Reason,
            term = e.Term,
            year = e.AcademicYear
        }).OrderBy(e => e.date).ToList();

        return Ok(new { success = true, excuses = result });
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin,Secretary,StageSupervisor")]
    public async Task<IActionResult> ApproveExcuse(int id)
    {
        var excuse = await _uow.Excuses.GetByIdAsync(id);
        if (excuse == null) return NotFound(new { success = false, message = "العذر غير موجود." });
        if (excuse.Status != "Pending") return BadRequest(new { success = false, message = "هذا العذر تمت مراجعته مسبقاً." });

        excuse.Status = "Approved";
        _uow.Excuses.Update(excuse);

        // إضافة سجل غياب كـ "Present" و IsExcused = true
        var existingAttendance = await _uow.AttendanceRecords.FindAsync(a => 
            a.StudentId == excuse.StudentId && a.Date.Date == excuse.Date.Date);

        if (!existingAttendance.Any())
        {
            await _uow.AttendanceRecords.AddAsync(new AttendanceRecord
            {
                StudentId = excuse.StudentId,
                Date = excuse.Date,
                Status = "Present", // يعتبر حاضر بس بعذر
                IsExcused = true,
                Term = excuse.Term,
                AcademicYear = excuse.AcademicYear
            });
        }
        else
        {
            var record = existingAttendance.First();
            record.Status = "Present";
            record.IsExcused = true;
            _uow.AttendanceRecords.Update(record);
        }

        await _uow.CompleteAsync();

        // تحديث درجات الحضور للطالب (حد أقصى 5 درجات)
        var allRecords = await _uow.AttendanceRecords.FindAsync(a => 
            a.StudentId == excuse.StudentId && 
            a.Term == excuse.Term && 
            a.AcademicYear == excuse.AcademicYear);

        var presentCount = allRecords.Count(a => a.Status == "Present" || a.IsExcused);
        decimal calculatedScore = Math.Min(10, presentCount) * 0.5m;

        var sGrades = await _uow.StudentGrades.FindAsync(g => 
            g.StudentId == excuse.StudentId && 
            g.Term == excuse.Term);

        foreach (var grade in sGrades)
        {
            grade.AttendanceScore = calculatedScore;
            _uow.StudentGrades.Update(grade);
        }

        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تمت الموافقة على العذر واحتساب درجة الحضور." });
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin,Secretary,StageSupervisor")]
    public async Task<IActionResult> RejectExcuse(int id)
    {
        var excuse = await _uow.Excuses.GetByIdAsync(id);
        if (excuse == null) return NotFound(new { success = false, message = "العذر غير موجود." });
        
        excuse.Status = "Rejected";
        _uow.Excuses.Update(excuse);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم رفض العذر." });
    }
}

public class CreateExcuseDto
{
    public int StudentId { get; set; }
    public DateTime Date { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
}
