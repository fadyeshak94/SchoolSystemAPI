using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using SchoolSystemAPI.Services;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class GradesController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IResultsService _resultsService;

    public GradesController(IUnitOfWork uow, IResultsService resultsService)
    {
        _uow = uow;
        _resultsService = resultsService;
    }

    [HttpGet("allowed-subjects")]
    public async Task<IActionResult> GetAllowedSubjects([FromQuery] int classId)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Secretary";
        var allSubjects = new[] { "ألحان", "قبطي", "أجبية", "مواد_متغيرة", "طقس" };
        
        if (role == "Admin" || role == "Secretary" || role == "StageSupervisor" || role == "User")
        {
            return Ok(allSubjects);
        }

        if (role == "Servant")
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var assignments = await _uow.ServantAssignments.FindAsync(sa => sa.UserId == userId && sa.ClassRoomId == classId);
                var assignedSubjects = assignments.Select(a => a.SubjectName).Distinct().ToList();
                return Ok(assignedSubjects);
            }
        }
        return Ok(new string[0]);
    }

    // 1. جلب درجات مادة معينة لتيرم معين (لصفحة GridView)
    [HttpGet("grid")]
    public async Task<IActionResult> GetGridData([FromQuery] int classId, [FromQuery] string term, [FromQuery] string subject, [FromQuery] string? academicYear)
    {
        if (!await IsUserAuthorizedForClassAndSubject(classId, subject))
        {
            return Forbid();
        }

        var appSettings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        var resolvedYear = !string.IsNullOrEmpty(academicYear) ? academicYear : (appSettings?.AcademicYear ?? "2024/2025");

        var students = await _uow.Students.GetQueryable()
            .Where(s => s.Enrollments.Any(e => e.ClassRoomId == classId && e.AcademicYear == resolvedYear))
            .ToListAsync();
        var studentIds = students.Select(s => s.Id).ToList();

        var grades = (await _uow.StudentGrades
            .FindAsync(g => studentIds.Contains(g.StudentId) && g.AcademicYear == resolvedYear))
            .Where(g => NormalizeTerm(g.Term) == NormalizeTerm(term) && NormalizeSubject(g.SubjectName) == NormalizeSubject(subject))
            .ToList();

        var result = students.Select(s => new
        {
            id = s.Id,
            name = s.Name,
            exam = grades.FirstOrDefault(g => g.StudentId == s.Id)?.ExamScore ?? 0,
            attendance = grades.FirstOrDefault(g => g.StudentId == s.Id)?.AttendanceScore ?? 0,
            total = (grades.FirstOrDefault(g => g.StudentId == s.Id)?.ExamScore ?? 0) + 
                    (grades.FirstOrDefault(g => g.StudentId == s.Id)?.AttendanceScore ?? 0)
        }).OrderBy(s => s.name).ToList();

        return Ok(new { students = result });
    }

    // 2. تحديث درجة الامتحان (ExamScore) فقط لعدة طلاب معاً
    [HttpPost("grid/save")]
    public async Task<IActionResult> SaveGridUpdates([FromBody] SaveGridDto request)
    {
        if (!await IsUserAuthorizedForClassAndSubject(request.ClassId, request.Subject))
        {
            return StatusCode(403, new { success = false, message = "غير مصرح لك برصد درجات هذا الفصل والمادة" });
        }

        var appSettings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        var resolvedYear = !string.IsNullOrEmpty(request.AcademicYear) ? request.AcademicYear : (appSettings?.AcademicYear ?? "2024/2025");

        var students = await _uow.Students.GetQueryable()
            .Where(s => s.Enrollments.Any(e => e.ClassRoomId == request.ClassId && e.AcademicYear == resolvedYear))
            .ToListAsync();
        var studentIds = students.Select(s => s.Id).ToList();

        var existingGrades = (await _uow.StudentGrades
            .FindAsync(g => studentIds.Contains(g.StudentId)))
            .Where(g => NormalizeTerm(g.Term) == NormalizeTerm(request.Term) && NormalizeSubject(g.SubjectName) == NormalizeSubject(request.Subject))
            .ToList();

        var normalizedTerm = NormalizeTerm(request.Term);
        var normalizedSubject = NormalizeSubject(request.Subject);

        var attendanceRecords = (await _uow.AttendanceRecords
            .FindAsync(a => studentIds.Contains(a.StudentId) && a.AcademicYear == resolvedYear))
            .Where(a => NormalizeTerm(a.Term) == normalizedTerm)
            .ToList();

        var classRoom = (await _uow.ClassRooms.FindAsync(c => c.Id == request.ClassId)).FirstOrDefault();
        var studentStage = classRoom?.Stage;

        int savedCount = 0;

        foreach (var update in request.Updates)
        {
            // منع إدخال درجة أكبر من 45 (درجة الامتحان العظمى)
            if (update.ExamScore > 45 || update.ExamScore < 0) continue;

            var attendanceScore = CalculateAttendanceScore(
                attendanceRecords, update.StudentId, resolvedYear, normalizedTerm, normalizedSubject, studentStage);

            var grade = existingGrades.FirstOrDefault(g => g.StudentId == update.StudentId && g.AcademicYear == resolvedYear)
                ?? existingGrades.FirstOrDefault(g => g.StudentId == update.StudentId);
            if (grade != null)
            {
                grade.ExamScore = update.ExamScore;
                grade.AcademicYear = resolvedYear;
                grade.AttendanceScore = attendanceScore;
                _uow.StudentGrades.Update(grade);
            }
            else
            {
                await _uow.StudentGrades.AddAsync(new StudentGrade
                {
                    StudentId = update.StudentId,
                    Term = request.Term,
                    SubjectName = request.Subject,
                    AcademicYear = resolvedYear,
                    ExamScore = update.ExamScore,
                    AttendanceScore = attendanceScore
                });
            }
            savedCount++;
        }

        await _uow.CompleteAsync();
        return Ok(new { success = true, message = $"تم تحديث {savedCount} درجة بنجاح" });
    }

    [HttpGet("class/{classId}/full")]
    public async Task<IActionResult> GetFullClassGrades(int classId, [FromQuery] string? term, [FromQuery] string? academicYear)
    {
        var appSettings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        var resolvedYear = !string.IsNullOrEmpty(academicYear) ? academicYear : (appSettings?.AcademicYear ?? "2024/2025");

        var students = await _uow.Students.GetQueryable()
            .Where(s => s.Enrollments.Any(e => e.ClassRoomId == classId && e.AcademicYear == resolvedYear))
            .ToListAsync();
        var studentIds = students.Select(s => s.Id).ToList();
        var grades = await _uow.StudentGrades.FindAsync(g => studentIds.Contains(g.StudentId) && g.AcademicYear == resolvedYear);

        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Secretary";
        var classRoom = (await _uow.ClassRooms.FindAsync(c => c.Id == classId)).FirstOrDefault();
        string stage = classRoom?.Stage ?? "ابتدائي";
        bool isFourSubjects = stage.Contains("ابتدائي ب") || stage.Contains("إعدادي") || stage.Contains("اعدادي") || stage.Contains("كبار");
        var normalizedTerm = NormalizeTerm(term);
        
        List<string> subjects = new List<string> { "أجبية", "ألحان", "قبطي" };
        if (isFourSubjects)
        {
            if (normalizedTerm == "ت1") subjects.Add("طقس");
            else if (normalizedTerm == "ت2") subjects.Add("مواد متغيرة");
            else { subjects.Add("طقس"); subjects.Add("مواد متغيرة"); }
        }
        else
        {
            subjects.Add("طقس"); subjects.Add("مواد متغيرة");
        }

        if (role == "Servant")
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var assignments = await _uow.ServantAssignments.FindAsync(sa => sa.UserId == userId && sa.ClassRoomId == classId);
                subjects = assignments.Select(a => a.SubjectName).Distinct().ToList();
            }
            else
            {
                subjects.Clear();
            }
        }

        var result = students.Select(s =>
        {
            var studentGrades = grades.Where(g => g.StudentId == s.Id && (string.IsNullOrEmpty(normalizedTerm) || NormalizeTerm(g.Term) == normalizedTerm)).ToList();
            var subDict = new Dictionary<string, decimal>();
            decimal total = 0;

            foreach (var sub in subjects)
            {
                var normSub = NormalizeSubject(sub);
                var score = studentGrades.Where(g => NormalizeSubject(g.SubjectName) == normSub).Sum(g => g.ExamScore + g.AttendanceScore);
                subDict[sub] = score;
                total += score;
            }

            return new
            {
                id = s.Id,
                name = s.Name,
                subjects = subDict,
                total = total
            };
        }).OrderBy(s => s.name).ToList();

        return Ok(new { success = true, subjects = subjects, students = result });
    }

    [HttpPost("saveAll")]
    public async Task<IActionResult> SaveAllGrades([FromBody] SaveAllGradesDto request)
    {
        var appSettings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        var resolvedYear = !string.IsNullOrEmpty(request.AcademicYear) ? request.AcademicYear : (appSettings?.AcademicYear ?? "2024/2025");

        var students = await _uow.Students.GetQueryable()
            .Where(s => s.Enrollments.Any(e => e.ClassRoomId == request.ClassId && e.AcademicYear == resolvedYear))
            .ToListAsync();
        var studentIds = students.Select(s => s.Id).ToList();
        var grades = (await _uow.StudentGrades.FindAsync(g => studentIds.Contains(g.StudentId))).ToList();

        int savedCount = 0;
        var normalizedTerm = NormalizeTerm(request.Term);

        var attendanceRecords = (await _uow.AttendanceRecords
            .FindAsync(a => studentIds.Contains(a.StudentId) && a.AcademicYear == resolvedYear))
            .Where(a => NormalizeTerm(a.Term) == normalizedTerm)
            .ToList();

        var classRoom = (await _uow.ClassRooms.FindAsync(c => c.Id == request.ClassId)).FirstOrDefault();
        var studentStage = classRoom?.Stage;

        foreach (var (studentId, subjectsDict) in request.Updates)
        {
            foreach (var (subject, score) in subjectsDict)
            {
                var normSub = NormalizeSubject(subject);
                var attendanceScore = CalculateAttendanceScore(
                    attendanceRecords, studentId, resolvedYear, normalizedTerm, normSub, studentStage);

                var grade = grades.FirstOrDefault(g => g.StudentId == studentId && NormalizeSubject(g.SubjectName) == normSub && NormalizeTerm(g.Term) == normalizedTerm && g.AcademicYear == resolvedYear)
                    ?? grades.FirstOrDefault(g => g.StudentId == studentId && NormalizeSubject(g.SubjectName) == normSub && NormalizeTerm(g.Term) == normalizedTerm);
                
                if (grade != null)
                {
                    grade.ExamScore = score;
                    grade.AcademicYear = resolvedYear;
                    grade.AttendanceScore = attendanceScore;
                    _uow.StudentGrades.Update(grade);
                }
                else
                {
                    grade = new StudentGrade
                    {
                        StudentId = studentId,
                        SubjectName = subject,
                        Term = request.Term,
                        AcademicYear = resolvedYear,
                        ExamScore = score,
                        AttendanceScore = attendanceScore
                    };
                    await _uow.StudentGrades.AddAsync(grade);
                    grades.Add(grade);
                }
                savedCount++;
            }
        }

        await _uow.CompleteAsync();
        return Ok(new { success = true, message = $"تم حفظ {savedCount} تعديل بنجاح" });
    }

    private async Task<bool> IsUserAuthorizedForClassAndSubject(int classId, string subject)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Secretary";
        if (role == "Admin" || role == "Secretary" || role == "User") return true;

        if (role == "StageSupervisor")
        {
            var stageAccess = User.FindFirst("StageAccess")?.Value;
            var classroom = (await _uow.ClassRooms.FindAsync(c => c.Id == classId)).FirstOrDefault();
            return classroom != null && (classroom.Stage ?? "").Trim().Equals(stageAccess?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        if (role == "Servant")
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var normSub = NormalizeSubject(subject);
                var assignments = await _uow.ServantAssignments.FindAsync(sa => 
                    sa.UserId == userId && sa.ClassRoomId == classId);
                return assignments.Any(sa => NormalizeSubject(sa.SubjectName) == normSub);
            }
        }

        return false;
    }

    private static decimal CalculateAttendanceScore(
        IEnumerable<AttendanceRecord> attendanceRecords,
        int studentId,
        string academicYear,
        string normalizedTerm,
        string normalizedSubject,
        string? studentStage)
    {
        if (academicYear == "2025/2026" && studentStage == "ابتدائي ب")
        {
            if ((normalizedSubject == "طقس" && normalizedTerm == "ت2") ||
                (normalizedSubject == "مواد متغيرة" && normalizedTerm == "ت1"))
            {
                return 0;
            }
        }

        if (studentStage == "ابتدائي أ" && normalizedSubject == "مواد متغيرة")
        {
            return 0;
        }

        var presentCount = attendanceRecords.Count(a =>
            a.StudentId == studentId && (a.Status == "Present" || a.IsExcused));
        return Math.Min(10, presentCount) * 0.5m;
    }

    private static string NormalizeSubject(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) return string.Empty;
        return subject.Trim()
            .Replace("أ", "ا")
            .Replace("إ", "ا")
            .Replace("آ", "ا")
            .Replace("_", " ");
    }

    private static string NormalizeTerm(string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) return string.Empty;
        var t = term.Trim();
        if (t == "1" || t == "ت1") return "ت1";
        if (t == "2" || t == "ت2") return "ت2";
        return t;
    }
}

public class SaveGridDto
{
    public int ClassId { get; set; }
    public string Term { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public List<GradeUpdateDto> Updates { get; set; } = new();
}

public class GradeUpdateDto
{
    public int StudentId { get; set; }
    public decimal ExamScore { get; set; }
}

public class SaveAllGradesDto
{
    public int ClassId { get; set; }
    public string Term { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public Dictionary<int, Dictionary<string, decimal>> Updates { get; set; } = new();
}

