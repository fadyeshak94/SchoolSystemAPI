using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        var allSubjects = new[] { "عقيدة", "طقس", "كتاب", "تاريخ", "حفظ_ومحفوظات", "سلوك", "حضور" };
        
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
    public async Task<IActionResult> GetGridData([FromQuery] int classId, [FromQuery] string term, [FromQuery] string subject)
    {
        if (!await IsUserAuthorizedForClassAndSubject(classId, subject))
        {
            return Forbid();
        }

        var students = await _uow.Students.FindAsync(s => s.ClassRoomId == classId);
        var studentIds = students.Select(s => s.Id).ToList();

        var grades = await _uow.StudentGrades
            .FindAsync(g => studentIds.Contains(g.StudentId) && g.Term == term && g.SubjectName == subject);

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

        var students = await _uow.Students.FindAsync(s => s.ClassRoomId == request.ClassId);
        var studentIds = students.Select(s => s.Id).ToList();

        var existingGrades = await _uow.StudentGrades
            .FindAsync(g => studentIds.Contains(g.StudentId) && g.Term == request.Term && g.SubjectName == request.Subject);

        int savedCount = 0;

        foreach (var update in request.Updates)
        {
            // منع إدخال درجة أكبر من 45 (درجة الامتحان العظمى)
            if (update.ExamScore > 45 || update.ExamScore < 0) continue;

            var grade = existingGrades.FirstOrDefault(g => g.StudentId == update.StudentId);
            if (grade != null)
            {
                grade.ExamScore = update.ExamScore;
                _uow.StudentGrades.Update(grade);
            }
            else
            {
                await _uow.StudentGrades.AddAsync(new StudentGrade
                {
                    StudentId = update.StudentId,
                    Term = request.Term,
                    SubjectName = request.Subject,
                    ExamScore = update.ExamScore,
                    AttendanceScore = 0 // الحضور بيتحدث من مكان تاني
                });
            }
            savedCount++;
        }

        await _uow.CompleteAsync();
        return Ok(new { success = true, message = $"تم تحديث {savedCount} درجة بنجاح" });
    }

    [HttpGet("class/{classId}/full")]
    public async Task<IActionResult> GetFullClassGrades(int classId, [FromQuery] string? term)
    {
        var students = await _uow.Students.FindAsync(s => s.ClassRoomId == classId);
        var studentIds = students.Select(s => s.Id).ToList();
        var grades = await _uow.StudentGrades.FindAsync(g => studentIds.Contains(g.StudentId));

        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Secretary";
        List<string> subjects = new List<string> { "أجبية", "الحان", "طقس", "قبطي", "مواد متغيرة" };

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
            var studentGrades = grades.Where(g => g.StudentId == s.Id && (string.IsNullOrEmpty(term) || g.Term == term)).ToList();
            var subDict = new Dictionary<string, decimal>();
            decimal total = 0;

            foreach (var sub in subjects)
            {
                var score = studentGrades.Where(g => g.SubjectName == sub).Sum(g => g.ExamScore + g.AttendanceScore);
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
        var students = await _uow.Students.FindAsync(s => s.ClassRoomId == request.ClassId);
        var studentIds = students.Select(s => s.Id).ToList();
        var grades = (await _uow.StudentGrades.FindAsync(g => studentIds.Contains(g.StudentId))).ToList();

        int savedCount = 0;

        foreach (var (studentId, subjectsDict) in request.Updates)
        {
            foreach (var (subject, score) in subjectsDict)
            {
                var grade = grades.FirstOrDefault(g => g.StudentId == studentId && g.SubjectName == subject && g.Term == request.Term);
                
                if (grade != null)
                {
                    grade.ExamScore = score;
                    _uow.StudentGrades.Update(grade);
                }
                else
                {
                    grade = new StudentGrade
                    {
                        StudentId = studentId,
                        SubjectName = subject,
                        Term = request.Term,
                        ExamScore = score,
                        AttendanceScore = 0
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
                var assignments = await _uow.ServantAssignments.FindAsync(sa => 
                    sa.UserId == userId && sa.ClassRoomId == classId && sa.SubjectName == subject);
                return assignments.Any();
            }
        }

        return false;
    }
}

public class SaveGridDto
{
    public int ClassId { get; set; }
    public string Term { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
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
    public Dictionary<int, Dictionary<string, decimal>> Updates { get; set; } = new();
}

