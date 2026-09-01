using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public ReportsController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet("classes-performance")]
    public async Task<IActionResult> GetClassesPerformance()
    {
        var classes = await _uow.ClassRooms.FindAsync(c => true);
        var students = await _uow.Students.FindAsync(s => true);

        var grades = await _uow.StudentGrades.FindAsync(g => true);

        var reportList = classes.Select(cls =>
        {
            var classStudents = students.Where(s => s.ClassRoomId == cls.Id).ToList();
            int totalStudents = classStudents.Count;
            int passedCount = 0;
            int failedCount = 0;
            int excellent = 0;
            int veryGood = 0;
            int good = 0;
            int acceptable = 0;

            decimal maxScore = cls.Stage.Contains("ابتدائي") ? 400m : 500m;

            foreach (var student in classStudents)
            {
                var studentGrades = grades.Where(g => g.StudentId == student.Id).ToList();
                decimal totalScore = studentGrades.Sum(g => g.ExamScore + g.AttendanceScore);
                
                decimal percentage = maxScore > 0 ? (totalScore / maxScore) * 100m : 0;

                // استبعاد الطلاب اللي جايبين أقل من 5% من الحسبة
                if (percentage < 5m)
                {
                    totalStudents--;
                    continue;
                }

                if (percentage >= 50m) passedCount++;
                else failedCount++;

                if (percentage >= 85m) excellent++;
                else if (percentage >= 75m) veryGood++;
                else if (percentage >= 65m) good++;
                else if (percentage >= 50m) acceptable++;
            }

            return new
            {
                classId = cls.Id,
                className = cls.Name,
                stage = cls.Stage,
                totalStudents = totalStudents,
                passedCount = passedCount,
                failedCount = failedCount,
                passPercentage = totalStudents > 0 ? Math.Round((decimal)passedCount / totalStudents * 100, 1) : 0,
                failPercentage = totalStudents > 0 ? Math.Round((decimal)failedCount / totalStudents * 100, 1) : 0,
                excellentCount = excellent,
                veryGoodCount = veryGood,
                goodCount = good,
                acceptableCount = acceptable,
                excellentPercentage = totalStudents > 0 ? Math.Round((decimal)excellent / totalStudents * 100, 1) : 0,
                veryGoodPercentage = totalStudents > 0 ? Math.Round((decimal)veryGood / totalStudents * 100, 1) : 0,
                goodPercentage = totalStudents > 0 ? Math.Round((decimal)good / totalStudents * 100, 1) : 0,
                acceptablePercentage = totalStudents > 0 ? Math.Round((decimal)acceptable / totalStudents * 100, 1) : 0
            };
        }).Where(r => r.totalStudents > 0).OrderBy(r => r.stage).ThenBy(r => r.className).ToList();

        return Ok(new { success = true, reports = reportList });
    }

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendanceReport()
    {
        // تحديد الطلاب اللي جايبين أقل من 5% لاستبعادهم
        var classes = await _uow.ClassRooms.FindAsync(c => true);
        var students = await _uow.Students.FindAsync(s => true);
        var grades = await _uow.StudentGrades.FindAsync(g => true);
        
        var excludedStudentIds = new HashSet<int>();
        foreach (var cls in classes)
        {
            decimal maxScore = cls.Stage.Contains("ابتدائي") ? 400m : 500m;
            var classStudents = students.Where(s => s.ClassRoomId == cls.Id);
            foreach (var student in classStudents)
            {
                decimal totalScore = grades.Where(g => g.StudentId == student.Id).Sum(g => g.ExamScore + g.AttendanceScore);
                decimal percentage = maxScore > 0 ? (totalScore / maxScore) * 100m : 0;
                if (percentage < 5m)
                {
                    excludedStudentIds.Add(student.Id);
                }
            }
        }

        var allRecords = await _uow.AttendanceRecords.FindAsync(a => true);
        
        // تصفية السجلات لاستبعاد هؤلاء الطلاب
        var records = allRecords.Where(a => !excludedStudentIds.Contains(a.StudentId)).ToList();
        
        var groupedByDate = records.GroupBy(a => new { Date = a.Date.Date, a.AcademicYear })
            .Select(g => new
            {
                date = g.Key.Date,
                academicYear = g.Key.AcademicYear,
                totalRecords = g.Count(),
                presentCount = g.Count(r => r.Status == "Present"),
                absentCount = g.Count(r => r.Status == "Absent"),
                excusedCount = g.Count(r => r.IsExcused)
            })
            .OrderByDescending(x => x.date)
            .ToList();

        var result = groupedByDate.Select(g => new
        {
            date = g.date.ToString("yyyy-MM-dd"),
            academicYear = g.academicYear,
            totalStudents = g.totalRecords,
            presentCount = g.presentCount,
            absentCount = g.absentCount,
            presentPercentage = g.totalRecords > 0 ? Math.Round((decimal)g.presentCount / g.totalRecords * 100, 1) : 0,
            absentPercentage = g.totalRecords > 0 ? Math.Round((decimal)g.absentCount / g.totalRecords * 100, 1) : 0
        });

        return Ok(new { success = true, reports = result });
    }
}
