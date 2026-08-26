using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public AttendanceController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // 1. جلب حالة الطلاب لتاريخ معين (عشان صفحة تسجيل الغياب)
    [HttpGet("entry")]
    public async Task<IActionResult> GetAttendanceEntryData([FromQuery] int classId, [FromQuery] DateTime date)
    {
        var students = await _uow.Students.FindAsync(s => s.ClassRoomId == classId);
        var studentIds = students.Select(s => s.Id).ToList();

        var existingRecords = await _uow.AttendanceRecords
            .FindAsync(a => studentIds.Contains(a.StudentId) && a.Date.Date == date.Date);

        var result = students.Select(s => new
        {
            id = s.Id,
            name = s.Name,
            // لو متسجل قبل كده هنجيب الحالة، لو لأ هيكون Present كافتراضي
            status = existingRecords.FirstOrDefault(a => a.StudentId == s.Id)?.Status ?? "Present"
        }).OrderBy(s => s.name).ToList();

        return Ok(new 
        { 
            students = result, 
            alreadyRecorded = existingRecords.Any() // لو في داتا سابقة هنبعت للواجهة تنبيه
        });
    }

    // 2. حفظ سجل الغياب للفصل بالكامل
    [HttpPost("entry/save")]
    public async Task<IActionResult> SaveAttendanceEntry([FromBody] SaveAttendanceDto request)
    {
        var studentIds = request.Records.Select(r => r.StudentId).ToList();
        
        var existingRecords = await _uow.AttendanceRecords
            .FindAsync(a => studentIds.Contains(a.StudentId) && a.Date.Date == request.Date.Date);

        int updatedCount = 0;
        int addedCount = 0;

        foreach (var record in request.Records)
        {
            var existing = existingRecords.FirstOrDefault(a => a.StudentId == record.StudentId);
            
            if (existing != null)
            {
                existing.Status = record.Status;
                existing.Term = request.Term;
                existing.AcademicYear = request.AcademicYear;
                _uow.AttendanceRecords.Update(existing);
                updatedCount++;
            }
            else
            {
                await _uow.AttendanceRecords.AddAsync(new AttendanceRecord
                {
                    StudentId = record.StudentId,
                    Date = request.Date,
                    Status = record.Status,
                    Term = request.Term,
                    AcademicYear = request.AcademicYear
                });
                addedCount++;
            }
        }

        await _uow.CompleteAsync();

        // تحديث درجات الحضور في جدول الدرجات
        var distinctStudentIds = studentIds.Distinct().ToList();
        var allRecords = await _uow.AttendanceRecords.FindAsync(a => 
            distinctStudentIds.Contains(a.StudentId) && 
            a.Term == request.Term && 
            a.AcademicYear == request.AcademicYear);
            
        var allGrades = await _uow.StudentGrades.FindAsync(g => 
            distinctStudentIds.Contains(g.StudentId) && 
            g.Term == request.Term);

        foreach (var sId in distinctStudentIds)
        {
            var presentCount = allRecords.Count(a => a.StudentId == sId && (a.Status == "Present" || a.IsExcused));
            decimal calculatedScore = Math.Min(10, presentCount) * 0.5m;

            var sGrades = allGrades.Where(g => g.StudentId == sId).ToList();
            foreach (var grade in sGrades)
            {
                grade.AttendanceScore = calculatedScore;
                _uow.StudentGrades.Update(grade);
            }
        }
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = $"تم حفظ الغياب: {addedCount} جديد، {updatedCount} تحديث وتم تحديث درجات الحضور." });
    }

    // 3. سجل الغياب التاريخي لطالب معين (لصفحة متابعة الغياب)
    [HttpGet("student/{studentId}/history")]
    public async Task<IActionResult> GetStudentHistory(int studentId)
    {
        var student = await _uow.Students.GetByIdAsync(studentId);
        if (student == null) return NotFound(new { error = "الطالب مش موجود" });

        var records = await _uow.AttendanceRecords
            .FindAsync(a => a.StudentId == studentId);

        var result = records.OrderByDescending(a => a.Date).Select(r => new
        {
            date = r.Date.ToString("yyyy-MM-dd"),
            term = r.Term,
            year = r.AcademicYear,
            status = r.Status
        }).ToList();

        return Ok(new { name = student.Name, records = result });
    }

    // 4. تقرير غياب الفصل بالكامل لتيرم معين
    [HttpGet("class/{classId}/term/{term}")]
    public async Task<IActionResult> GetAttendanceData(int classId, string term)
    {
        var students = await _uow.Students.FindAsync(s => s.ClassRoomId == classId);
        var studentIds = students.Select(s => s.Id).ToList();

        var records = await _uow.AttendanceRecords
            .FindAsync(a => studentIds.Contains(a.StudentId) && a.Term == term);

        var dateSet = records.Select(r => r.Date.ToString("yyyy-MM-dd")).Distinct().OrderBy(d => d).ToList();
        
        var result = students.Select(s =>
        {
            var studentRecords = records.Where(r => r.StudentId == s.Id).ToList();
            var dict = new Dictionary<string, string>();
            int presentCount = 0;
            
            foreach (var d in dateSet)
            {
                var st = studentRecords.FirstOrDefault(r => r.Date.ToString("yyyy-MM-dd") == d)?.Status ?? "";
                dict[d] = st;
                if (st.Equals("Present", StringComparison.OrdinalIgnoreCase)) presentCount++;
            }

            return new
            {
                id = s.Id,
                name = s.Name,
                records = dict,
                presentCount = presentCount,
                totalCount = dateSet.Count,
                percentage = dateSet.Count > 0 ? (presentCount / (double)dateSet.Count * 100) : 0
            };
        }).OrderBy(s => s.name).ToList();

        return Ok(new { dates = dateSet, students = result });
    }
}

public class SaveAttendanceDto
{
    public int ClassId { get; set; }
    public DateTime Date { get; set; }
    public string Term { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public List<AttendanceRecordDto> Records { get; set; } = new();
}

public class AttendanceRecordDto
{
    public int StudentId { get; set; }
    public string Status { get; set; } = string.Empty; // "Present" or "Absent"
}
