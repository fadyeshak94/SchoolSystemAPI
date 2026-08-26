using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public DashboardController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardStats()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var classIdStr = User.FindFirst("ClassRoomId")?.Value;
        var stageAccess = User.FindFirst("StageAccess")?.Value;

        var allStudents = await _uow.Students.FindAsync(s => true);
        var allClasses = await _uow.ClassRooms.FindAsync(c => true);

        IEnumerable<Models.Student> students = allStudents;
        IEnumerable<Models.ClassRoom> classes = allClasses;

        if (role != "Admin")
        {
            if (!string.IsNullOrEmpty(stageAccess))
            {
                classes = classes.Where(c => c.Stage == stageAccess);
                var classIds = classes.Select(c => c.Id).ToList();
                students = students.Where(s => classIds.Contains(s.ClassRoomId));
            }
            else if (!string.IsNullOrEmpty(classIdStr) && int.TryParse(classIdStr, out int classId))
            {
                classes = classes.Where(c => c.Id == classId);
                students = students.Where(s => s.ClassRoomId == classId);
            }
        }

        return Ok(new
        {
            scopeLabel = role == "Admin" ? "النظام بالكامل" : (!string.IsNullOrEmpty(stageAccess) ? $"مرحلة {stageAccess}" : "فصلك فقط"),
            studentsCount = students.Count(),
            maleCount = students.Count(s => s.Gender == "ذكر"),
            femaleCount = students.Count(s => s.Gender == "أنثى"),
            deaconCount = students.Count(s => s.IsDeacon),
            classesCount = classes.Count(),
            classStats = classes.Select(c => new
            {
                name = c.Name,
                stage = c.Stage,
                total = students.Count(s => s.ClassRoomId == c.Id),
                male = students.Count(s => s.ClassRoomId == c.Id && s.Gender == "ذكر"),
                female = students.Count(s => s.ClassRoomId == c.Id && s.Gender == "أنثى"),
                deacon = students.Count(s => s.ClassRoomId == c.Id && s.IsDeacon)
            }).ToList()
        });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] int? classId)
    {
        var allStudents = await _uow.Students.FindAsync(s => true);
        if (classId.HasValue)
        {
            allStudents = allStudents.Where(s => s.ClassRoomId == classId.Value);
        }

        var studentIds = allStudents.Select(s => s.Id).ToList();
        var allGrades = await _uow.StudentGrades.FindAsync(g => studentIds.Contains(g.StudentId));

        var subjects = new[] { "أجبية", "الحان", "طقس", "قبطي", "مواد متغيرة" };
        var results = new List<object>();

        foreach (var sub in subjects)
        {
            var subGradesT1 = allGrades.Where(g => g.SubjectName == sub && g.Term == "ت1").ToList();
            var subGradesT2 = allGrades.Where(g => g.SubjectName == sub && g.Term == "ت2").ToList();

            int totalT1 = subGradesT1.Count;
            int passedT1 = subGradesT1.Count(g => (g.ExamScore + g.AttendanceScore) >= 25);
            
            int totalT2 = subGradesT2.Count;
            int passedT2 = subGradesT2.Count(g => (g.ExamScore + g.AttendanceScore) >= 25);

            double? passRateT1 = totalT1 > 0 ? (passedT1 / (double)totalT1) * 100 : null;
            double? passRateT2 = totalT2 > 0 ? (passedT2 / (double)totalT2) * 100 : null;
            
            var overallTotal = totalT1 + totalT2;
            var overallPassed = passedT1 + passedT2;
            double? overallPassRate = overallTotal > 0 ? (overallPassed / (double)overallTotal) * 100 : null;

            results.Add(new
            {
                label = sub,
                term1PassRate = passRateT1,
                term2PassRate = passRateT2,
                overallPassRate = overallPassRate,
                totalStudents = Math.Max(totalT1, totalT2)
            });
        }

        return Ok(new { subjects = results });
    }

    [HttpGet("renewals")]
    public async Task<IActionResult> GetRenewals()
    {
        // For demonstration, implementing simple response. Real logic filters passed students.
        var allStudents = await _uow.Students.FindAsync(s => true);
        var allClasses = await _uow.ClassRooms.FindAsync(c => true);

        var classes = allClasses.Select(c => new
        {
            className = c.Name,
            stage = c.Stage,
            year = c.Year,
            totalStudents = allStudents.Count(s => s.ClassRoomId == c.Id),
            withPhone = allStudents.Count(s => s.ClassRoomId == c.Id && !string.IsNullOrEmpty(s.PhonesJson) && s.PhonesJson != "[]"),
            students = allStudents.Where(s => s.ClassRoomId == c.Id).Select(s => new {
                id = s.Id,
                name = s.Name,
                phone = s.PhonesJson
            }).ToList()
        }).ToList();

        return Ok(new { 
            classes = classes,
            totalStudents = allStudents.Count(),
            totalWithPhone = classes.Sum(c => c.withPhone)
        });
    }
}
