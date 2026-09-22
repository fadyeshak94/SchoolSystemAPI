using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<IActionResult> GetAttendanceEntryData([FromQuery] int classId, [FromQuery] DateTime date, [FromQuery] string? academicYear)
    {
        var appSettings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        var resolvedYear = !string.IsNullOrEmpty(academicYear) ? academicYear : (appSettings?.AcademicYear ?? "2024/2025");

        var students = await _uow.Students.GetQueryable().Where(s => s.Enrollments.Any(e => e.ClassRoomId == classId && e.AcademicYear == resolvedYear)).ToListAsync();
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
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("Role")?.Value;
        if (role == "Servant")
        {
            return Forbid();
        }

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
    public async Task<IActionResult> GetAttendanceData(int classId, string term, [FromQuery] string? academicYear)
    {
        var appSettings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        var resolvedYear = !string.IsNullOrEmpty(academicYear) ? academicYear : (appSettings?.AcademicYear ?? "2024/2025");

        var students = await _uow.Students.GetQueryable().Where(s => s.Enrollments.Any(e => e.ClassRoomId == classId && e.AcademicYear == resolvedYear)).ToListAsync();
        var studentIds = students.Select(s => s.Id).ToList();

        var records = await _uow.AttendanceRecords
            .FindAsync(a => studentIds.Contains(a.StudentId) && a.Term == term && a.AcademicYear == resolvedYear);

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

    // 5. لوحة تحكم الغياب (Dashboard)
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardData([FromQuery] string term, [FromQuery] string? academicYear)
    {
        var appSettings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        var resolvedYear = !string.IsNullOrEmpty(academicYear) ? academicYear : (appSettings?.AcademicYear ?? "2024/2025");

        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("Role")?.Value;
        var userTitle = User.FindFirst("Title")?.Value ?? "";
        var userClassId = User.FindFirst("ClassRoomId")?.Value;
        var stageAccess = User.FindFirst("StageAccess")?.Value;

        var allClasses = await _uow.ClassRooms.FindAsync(c => true);
        var allowedClasses = allClasses.AsEnumerable();

        bool isTopManagement = userRole == "Admin" || userRole == "HeadSecretary" || userRole == "Principal" || userRole == "BoardMember" || userTitle.Contains("ناظر") || userTitle.Contains("عضو مجلس إدارة");

        if (!isTopManagement)
        {
            if (userRole == "StageSupervisor")
            {
                allowedClasses = allowedClasses.Where(c => !string.IsNullOrEmpty(stageAccess) && stageAccess.Contains(c.Stage));
            }
            else if (userRole == "Secretary" || userRole == "User")
            {
                allowedClasses = allowedClasses.Where(c => c.Id.ToString() == userClassId);
            }
            else
            {
                allowedClasses = allowedClasses.Where(c => c.Id.ToString() == userClassId);
            }
        }

        var classIds = allowedClasses.Select(c => c.Id).ToList();

        var normYear1 = resolvedYear.Replace("-", "/");
        var normYear2 = resolvedYear.Replace("/", "-");

        var students = await _uow.Students.GetQueryable()
            .Where(s => s.Enrollments.Any(e => classIds.Contains(e.ClassRoomId) && (e.AcademicYear == normYear1 || e.AcademicYear == normYear2)))
            .Include(s => s.Enrollments)
            .ToListAsync();

        var studentIds = students.Select(s => s.Id).ToList();

        string altTerm = term;
        if (term == "ت1") altTerm = "1";
        else if (term == "ت2") altTerm = "2";
        else if (term == "1") altTerm = "ت1";
        else if (term == "2") altTerm = "ت2";

        var records = await _uow.AttendanceRecords
            .FindAsync(a => studentIds.Contains(a.StudentId) && (a.Term == term || a.Term == altTerm) && (a.AcademicYear == normYear1 || a.AcademicYear == normYear2));

        var result = new List<object>();

        int totalSchoolStudents = 0;
        int totalSchoolPresent = 0;
        int totalSchoolAbsent = 0;
        int totalSchoolRecords = 0;

        foreach (var c in allowedClasses.OrderBy(c => c.Stage).ThenBy(c => c.Name))
        {
            var classStudents = students.Where(s => s.Enrollments.Any(e => e.ClassRoomId == c.Id && (e.AcademicYear == normYear1 || e.AcademicYear == normYear2))).ToList();
            if (!classStudents.Any()) continue;

            var classStudentIds = classStudents.Select(s => s.Id).ToList();
            var classRecords = records.Where(r => classStudentIds.Contains(r.StudentId)).ToList();

            int totalRecords = classRecords.Count;
            int presentCount = classRecords.Count(r => r.Status.Equals("Present", StringComparison.OrdinalIgnoreCase) || r.IsExcused);
            int absentCount = totalRecords - presentCount;

            totalSchoolStudents += classStudents.Count;
            totalSchoolPresent += presentCount;
            totalSchoolAbsent += absentCount;
            totalSchoolRecords += totalRecords;

            double overallPercentage = totalRecords > 0 ? (presentCount / (double)totalRecords) * 100 : 0;

            // Trend over time
            var dates = classRecords.Select(r => r.Date.ToString("yyyy-MM-dd")).Distinct().OrderBy(d => d).ToList();
            var history = new List<object>();

            foreach(var d in dates)
            {
                var dayRecords = classRecords.Where(r => r.Date.ToString("yyyy-MM-dd") == d).ToList();
                int dayTotal = dayRecords.Count;
                int dayPresent = dayRecords.Count(r => r.Status.Equals("Present", StringComparison.OrdinalIgnoreCase) || r.IsExcused);
                history.Add(new {
                    date = d,
                    percentage = dayTotal > 0 ? (dayPresent / (double)dayTotal) * 100 : 0
                });
            }

            result.Add(new {
                classId = c.Id,
                className = c.Name,
                stage = c.Stage,
                totalStudents = classStudents.Count,
                presentCount = presentCount,
                absentCount = absentCount,
                percentage = overallPercentage,
                history = history
            });
        }

        double schoolPercentage = totalSchoolRecords > 0 ? (totalSchoolPresent / (double)totalSchoolRecords) * 100 : 0;

        var summary = new {
            totalStudents = totalSchoolStudents,
            totalRecords = totalSchoolRecords,
            totalPresent = totalSchoolPresent,
            totalAbsent = totalSchoolAbsent,
            overallPercentage = schoolPercentage
        };

        return Ok(new { success = true, summary = summary, classes = result });
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
