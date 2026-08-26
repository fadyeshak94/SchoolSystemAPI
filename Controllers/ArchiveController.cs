using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ArchiveController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public ArchiveController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpPost("year")]
    public async Task<IActionResult> ArchiveYear([FromQuery] string year)
    {
        if (string.IsNullOrWhiteSpace(year))
            return BadRequest(new { success = false, message = "العام الدراسي مطلوب" });

        var students = await _uow.Students.FindAsync(s => true);
        var classes = await _uow.ClassRooms.FindAsync(c => true);
        var classMap = classes.ToDictionary(c => c.Id, c => c);

        var archivesToInsert = new List<StudentArchive>();

        foreach (var student in students)
        {
            var className = "غير معروف";
            var stageName = "";
            
            if (classMap.TryGetValue(student.ClassRoomId, out var classRoom))
            {
                className = classRoom.Name;
                stageName = classRoom.Stage;
            }

            archivesToInsert.Add(new StudentArchive
            {
                AcademicYear = year,
                OriginalStudentId = student.Id,
                StudentName = student.Name ?? "",
                ClassName = className,
                StageName = stageName,
                GovGrade = student.GovGrade ?? "",
                PhonesJson = student.PhonesJson ?? "[]"
            });
        }

        if (archivesToInsert.Any())
        {
            // Remove existing archive for this year if it exists to allow re-running
            var existing = await _uow.StudentArchives.FindAsync(a => a.AcademicYear == year);
            if (existing.Any())
            {
                foreach (var ex in existing)
                {
                    _uow.StudentArchives.Remove(ex);
                }
            }

            foreach(var archive in archivesToInsert)
            {
                await _uow.StudentArchives.AddAsync(archive);
            }
            
            await _uow.CompleteAsync();
        }

        return Ok(new { success = true, message = $"تم أرشفة {archivesToInsert.Count} طالب للعام {year} بنجاح" });
    }

    [HttpGet("years")]
    public async Task<IActionResult> GetArchivedYears()
    {
        var archives = await _uow.StudentArchives.FindAsync(a => true);
        var years = archives.Select(a => a.AcademicYear).Distinct().OrderByDescending(y => y).ToList();
        return Ok(new { success = true, years = years });
    }

    [HttpGet("classes")]
    public async Task<IActionResult> GetArchivedClasses([FromQuery] string year)
    {
        var archives = await _uow.StudentArchives.FindAsync(a => a.AcademicYear == year);
        var classes = archives
            .Select(a => new { a.ClassName, a.StageName })
            .Distinct()
            .OrderBy(c => c.StageName).ThenBy(c => c.ClassName)
            .ToList();
            
        return Ok(new { success = true, classes = classes });
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetArchivedStudents([FromQuery] string year, [FromQuery] string className)
    {
        var archives = await _uow.StudentArchives.FindAsync(a => a.AcademicYear == year && a.ClassName == className);
        var studentIds = archives.Select(a => a.OriginalStudentId).ToList();
        var grades = await _uow.StudentGrades.FindAsync(g => studentIds.Contains(g.StudentId));
        
        var result = archives.Select(a => {
            var total = grades.Where(g => g.StudentId == a.OriginalStudentId).Sum(g => g.ExamScore + g.AttendanceScore);
            return new {
                id = a.OriginalStudentId,
                name = a.StudentName,
                className = a.ClassName,
                stage = a.StageName,
                govGrade = a.GovGrade,
                phones = a.PhonesJson,
                totalGrades = total
            };
        }).OrderBy(s => s.name).ToList();

        return Ok(new { success = true, students = result });
    }

    [HttpGet("student-absence")]
    public async Task<IActionResult> GetStudentAbsence([FromQuery] int studentId, [FromQuery] string year)
    {
        var attendance = await _uow.AttendanceRecords.FindAsync(a => 
            a.StudentId == studentId && 
            (a.AcademicYear == year || string.IsNullOrEmpty(a.AcademicYear) || a.AcademicYear == null) && 
            (a.Status.ToLower() == "absent" || a.Status == "غائب"));
        
        var result = attendance.Select(a => new {
            date = a.Date.ToString("yyyy-MM-dd"),
            reason = "-" // No specific reason field in AttendanceRecord
        }).OrderBy(a => a.date).ToList();

        return Ok(new { success = true, absence = result });
    }

    [HttpGet("student-result")]
    public async Task<IActionResult> GetStudentResult([FromQuery] int studentId, [FromQuery] string year)
    {
        // Currently StudentGrade doesn't have an AcademicYear field, 
        // so we fetch all grades for this student ID.
        var grades = await _uow.StudentGrades.FindAsync(g => g.StudentId == studentId);
        
        var allSubjects = new[] { "أجبية", "الحان", "طقس", "قبطي", "مواد متغيرة" };
        var subjectsList = new List<object>();
        decimal total = 0;

        foreach (var sub in allSubjects)
        {
            var subTotal = grades.Where(g => g.SubjectName == sub).Sum(g => g.ExamScore + g.AttendanceScore);
            subjectsList.Add(new { name = sub, grade = subTotal });
            total += subTotal;
        }

        var result = new {
            subjects = subjectsList,
            total = total
        };

        return Ok(new { success = true, result = result });
    }
}
