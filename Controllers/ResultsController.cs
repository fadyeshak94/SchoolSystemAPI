using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using SchoolSystemAPI.Services;
using System.Text;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IResultsService _resultsService;

    public ResultsController(IUnitOfWork uow, IResultsService resultsService)
    {
        _uow = uow;
        _resultsService = resultsService;
    }

    [HttpGet("class/{classId}")]
    public async Task<IActionResult> GetClassResults(int classId, [FromQuery] string term, [FromQuery] string subject)
    {
        var students = await _uow.Students.FindAsync(s => s.ClassRoomId == classId);
        var studentIds = students.Select(s => s.Id).ToList();

        var gradesQuery = await _uow.StudentGrades.FindAsync(g => studentIds.Contains(g.StudentId));
        if (term != "all" && !string.IsNullOrEmpty(term))
        {
            gradesQuery = gradesQuery.Where(g => g.Term == term).ToList();
        }

        var classRoom = (await _uow.ClassRooms.FindAsync(c => c.Id == classId)).FirstOrDefault();
        string stage = classRoom?.Stage ?? "ابتدائي"; // Default to calculate max score

        List<object> columns = new List<object>();

        if (subject != "all" && !string.IsNullOrEmpty(subject))
        {
            gradesQuery = gradesQuery.Where(g => g.SubjectName == subject).ToList();
            columns.Add(new { key = "exam", label = "الامتحان" });
            columns.Add(new { key = "attendance", label = "الحضور" });
        }
        else
        {
            var allSubjects = new[] { "أجبية", "الحان", "طقس", "قبطي", "مواد متغيرة" };
            foreach (var sub in allSubjects)
            {
                columns.Add(new { key = sub, label = sub });
            }
        }

        var resultStudents = students.Select(s =>
        {
            var studentGrades = gradesQuery.Where(g => g.StudentId == s.Id).ToList();
            var colsDict = new Dictionary<string, object>();
            decimal total = 0;

            if (subject != "all" && !string.IsNullOrEmpty(subject))
            {
                var examScore = studentGrades.Sum(g => g.ExamScore);
                var attScore = studentGrades.Sum(g => g.AttendanceScore);
                colsDict["exam"] = examScore;
                colsDict["attendance"] = attScore;
                total = examScore + attScore;
            }
            else
            {
                var allSubjects = new[] { "أجبية", "الحان", "طقس", "قبطي", "مواد متغيرة" };
                foreach (var sub in allSubjects)
                {
                    var exam = studentGrades.Where(g => g.SubjectName == sub).Sum(g => g.ExamScore);
                    var att = studentGrades.Where(g => g.SubjectName == sub).Sum(g => g.AttendanceScore);
                    colsDict[sub] = new { exam = exam, att = att, total = exam + att };
                    total += exam + att;
                }
            }

            decimal percentage = _resultsService.CalculatePercentage(total, stage);

            return new
            {
                id = s.Id,
                name = s.Name,
                cols = colsDict,
                total = total,
                percentage = percentage
            };
        }).OrderByDescending(s => s.total).ToList(); // Sort by rank

        return Ok(new { success = true, columns = columns, students = resultStudents });
    }

    [HttpGet("class/{classId}/export")]
    public async Task<IActionResult> ExportClassResults(int classId)
    {
        // For now, generating a dummy text as base64 (since PDF generation needs complex setup)
        // You can replace this with actual PDF generation (e.g., QuestPDF) when ready
        var content = "This is a placeholder for the PDF results export.";
        var bytes = Encoding.UTF8.GetBytes(content);
        var base64 = Convert.ToBase64String(bytes);

        return Ok(new { 
            success = true, 
            base64 = base64, 
            filename = $"Results_Class_{classId}.pdf" 
        });
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetStudentResult(int studentId)
    {
        var student = await _uow.Students.GetByIdAsync(studentId);
        if (student == null) return NotFound(new { success = false, message = "الطالب غير موجود" });

        var grades = await _uow.StudentGrades.FindAsync(g => g.StudentId == studentId);
        var classRoom = (await _uow.ClassRooms.FindAsync(c => c.Id == student.ClassRoomId)).FirstOrDefault();
        string stage = classRoom?.Stage ?? "ابتدائي";

        decimal total = 0;
        var allSubjects = new[] { "أجبية", "الحان", "طقس", "قبطي", "مواد متغيرة" };
        foreach (var sub in allSubjects)
        {
            var exam = grades.Where(g => g.SubjectName == sub).Sum(g => g.ExamScore);
            var att = grades.Where(g => g.SubjectName == sub).Sum(g => g.AttendanceScore);
            total += exam + att;
        }

        decimal percentage = _resultsService.CalculatePercentage(total, stage);

        // بناءً على طلبك: افتراض أن جميع المشتركين دفعوا في السنة الماضية
        decimal debt = 0; // تم إيقاف المديونية بناءً على رغبتك

        return Ok(new
        {
            success = true,
            total = total,
            percentage = percentage,
            isPassed = percentage >= 50,
            hasDebt = debt > 0,
            debtAmount = debt
        });
    }
}
