using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,HeadSecretary")]
public class ReportsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ApplicationDbContext _context;

    public ReportsController(IUnitOfWork uow, ApplicationDbContext context)
    {
        _uow = uow;
        _context = context;
    }

    [HttpGet("classes-performance")]
    public async Task<IActionResult> GetClassesPerformance([FromQuery] string? academicYear)
    {
        var classes = await _uow.ClassRooms.FindAsync(c => true);
        var students = await _uow.Students.GetQueryable().Include(s => s.Enrollments).ToListAsync();

        var grades = await _uow.StudentGrades.FindAsync(g => true);

        var reportList = classes.Select(cls =>
        {
            var classStudents = students.Where(s => s.Enrollments.Any(e => e.ClassRoomId == cls.Id && (string.IsNullOrEmpty(academicYear) || e.AcademicYear == academicYear))).ToList();
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
    public async Task<IActionResult> GetAttendanceReport([FromQuery] string? academicYear)
    {
        // تحديد الطلاب اللي جايبين أقل من 5% لاستبعادهم
        var classes = await _uow.ClassRooms.FindAsync(c => true);
        var students = await _uow.Students.GetQueryable().Include(s => s.Enrollments).ToListAsync();
        var grades = await _uow.StudentGrades.FindAsync(g => true);
        
        var excludedStudentIds = new HashSet<int>();
        foreach (var cls in classes)
        {
            decimal maxScore = cls.Stage.Contains("ابتدائي") ? 400m : 500m;
            var classStudents = students.Where(s => s.Enrollments.Any(e => e.ClassRoomId == cls.Id && (string.IsNullOrEmpty(academicYear) || e.AcademicYear == academicYear)));
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

        var allRecords = await _uow.AttendanceRecords.FindAsync(a => string.IsNullOrEmpty(academicYear) || a.AcademicYear == academicYear);
        
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

    [HttpGet("daily-revenue")]
    public async Task<IActionResult> GetDailyRevenue([FromQuery] string? from, [FromQuery] string? to)
    {
        var paymentsQuery = _uow.SubscriptionPayments.GetQueryable();
        var pendingQuery = _uow.PendingRegistrations.GetQueryable().Where(p => p.Status == "Pending" || p.Status == "Waitlisted");
        var financeQuery = _uow.FinancialTransactions.GetQueryable();
        
        if (!string.IsNullOrEmpty(from) && DateTime.TryParse(from, out DateTime fromDate))
        {
            paymentsQuery = paymentsQuery.Where(p => p.PaymentDate >= fromDate.Date);
            pendingQuery = pendingQuery.Where(p => p.RequestDate >= fromDate.Date);
            financeQuery = financeQuery.Where(f => f.TransactionDate >= fromDate.Date);
        }
        
        if (!string.IsNullOrEmpty(to) && DateTime.TryParse(to, out DateTime toDate))
        {
            var endOfDay = toDate.Date.AddDays(1).AddTicks(-1);
            paymentsQuery = paymentsQuery.Where(p => p.PaymentDate <= endOfDay);
            pendingQuery = pendingQuery.Where(p => p.RequestDate <= endOfDay);
            financeQuery = financeQuery.Where(f => f.TransactionDate <= endOfDay);
        }

        var payments = await paymentsQuery.ToListAsync();
        var pendings = await pendingQuery.ToListAsync();
        var finances = await financeQuery.ToListAsync();

        var allTransactions = new List<object>();

        allTransactions.AddRange(payments.Select(p => new {
            Date = p.PaymentDate.ToString("yyyy-MM-dd"),
            Amount = p.Amount,
            Description = "اشتراكات",
            Type = "Revenue"
        }));

        allTransactions.AddRange(pendings.Select(p => new {
            Date = p.RequestDate.ToString("yyyy-MM-dd"),
            Amount = p.AmountPaid,
            Description = "اشتراكات",
            Type = "Revenue"
        }));

        allTransactions.AddRange(finances.Select(f => new {
            Date = f.TransactionDate.ToString("yyyy-MM-dd"),
            Amount = f.Amount,
            Description = f.Description,
            Type = f.Type
        }));

        // Sort all by date descending
        var sorted = allTransactions.OrderByDescending(t => (string)((dynamic)t).Date).ToList();

        return Ok(new { success = true, data = sorted });
    }

    [HttpGet("at-risk")]
    public async Task<IActionResult> GetAtRiskStudents([FromQuery] string? academicYear)
    {
        var students = await _uow.Students.GetQueryable().Include(s => s.Enrollments).ThenInclude(e => e.ClassRoom).ToListAsync();
        var allRecords = await _uow.AttendanceRecords.FindAsync(a => string.IsNullOrEmpty(academicYear) || a.AcademicYear == academicYear);
        var allGrades = await _uow.StudentGrades.FindAsync(g => string.IsNullOrEmpty(academicYear) || g.AcademicYear == academicYear);

        var result = new List<object>();

        foreach (var student in students)
        {
            var stRecords = allRecords.Where(r => r.StudentId == student.Id).ToList();
            var stGrades = allGrades.Where(g => g.StudentId == student.Id).ToList();
            
            var currentEnrollment = student.Enrollments.OrderByDescending(e => e.Id).FirstOrDefault(e => string.IsNullOrEmpty(academicYear) || e.AcademicYear == academicYear);
            if (currentEnrollment == null) continue;

            int totalDays = stRecords.Count;
            int absentDays = stRecords.Count(r => r.Status == "Absent" && !r.IsExcused);
            decimal absencePercentage = totalDays > 0 ? (decimal)absentDays / totalDays * 100m : 0;

            decimal totalScore = stGrades.Sum(g => g.TotalScore);
            decimal maxScore = currentEnrollment.ClassRoom.Stage.Contains("ابتدائي") ? 400m : 500m;
            decimal gradePercentage = maxScore > 0 ? (totalScore / maxScore) * 100m : 0;

            // Only add if there is some data
            if (totalDays > 0 || stGrades.Count > 0)
            {
                result.Add(new
                {
                    studentId = student.Id,
                    studentName = student.Name,
                    className = currentEnrollment.ClassRoom.Name,
                    absencePercentage = Math.Round(absencePercentage, 1),
                    gradePercentage = Math.Round(gradePercentage, 1)
                });
            }
        }

        return Ok(new { success = true, data = result });
    }

    [HttpGet("consecutive-absences")]
    public async Task<IActionResult> GetConsecutiveAbsences([FromQuery] string? academicYear, [FromQuery] int threshold = 2)
    {
        var students = await _uow.Students.GetQueryable().Include(s => s.Enrollments).ThenInclude(e => e.ClassRoom).ToListAsync();
        var allRecords = await _uow.AttendanceRecords.FindAsync(a => string.IsNullOrEmpty(academicYear) || a.AcademicYear == academicYear);
        
        var result = new List<object>();

        foreach (var student in students)
        {
            var stRecords = allRecords.Where(r => r.StudentId == student.Id).OrderBy(r => r.Date).ToList();
            int consecutiveCount = 0;
            int maxConsecutive = 0;
            List<string> absenceDates = new();

            foreach (var record in stRecords)
            {
                if (record.Status == "Absent" && !record.IsExcused)
                {
                    consecutiveCount++;
                    absenceDates.Add(record.Date.ToString("yyyy-MM-dd"));
                    if (consecutiveCount > maxConsecutive) maxConsecutive = consecutiveCount;
                }
                else
                {
                    if (consecutiveCount >= threshold)
                    {
                        // Already found a sequence >= threshold, keep max but don't reset to 0 if we want to report the maximum block.
                        // Or we can just keep tracking.
                    }
                    else
                    {
                        consecutiveCount = 0;
                        absenceDates.Clear();
                    }
                }
            }

            if (maxConsecutive >= threshold)
            {
                var currentEnrollment = student.Enrollments.OrderByDescending(e => e.Id).FirstOrDefault(e => string.IsNullOrEmpty(academicYear) || e.AcademicYear == academicYear);
                result.Add(new
                {
                    studentId = student.Id,
                    studentName = student.Name,
                    className = currentEnrollment?.ClassRoom?.Name ?? "غير مسجل",
                    consecutiveAbsencesCount = maxConsecutive,
                    lastAbsenceDates = absenceDates.TakeLast(threshold)
                });
            }
        }

        return Ok(new { success = true, data = result.OrderByDescending(r => ((dynamic)r).consecutiveAbsencesCount) });
    }

    [HttpGet("student-profile/{studentId}")]
    public async Task<IActionResult> GetStudentProfile(int studentId, [FromQuery] string? academicYear)
    {
        var student = await _uow.Students.GetQueryable().Include(s => s.Enrollments).ThenInclude(e => e.ClassRoom).Include(s => s.Mother).FirstOrDefaultAsync(s => s.Id == studentId);
        if (student == null) return NotFound(new { success = false, message = "الطالب غير موجود" });

        var stRecords = await _uow.AttendanceRecords.FindAsync(a => a.StudentId == studentId && (string.IsNullOrEmpty(academicYear) || a.AcademicYear == academicYear));
        var stGrades = await _uow.StudentGrades.FindAsync(g => g.StudentId == studentId && (string.IsNullOrEmpty(academicYear) || g.AcademicYear == academicYear));
        var stTransactions = await _uow.SubscriptionPayments.FindAsync(p => p.StudentId == studentId);

        var currentEnrollment = student.Enrollments.OrderByDescending(e => e.Id).FirstOrDefault(e => string.IsNullOrEmpty(academicYear) || e.AcademicYear == academicYear);

        int totalDays = stRecords.Count();
        int presentDays = stRecords.Count(r => r.Status == "Present");
        int absentDays = stRecords.Count(r => r.Status == "Absent");
        int excusedDays = stRecords.Count(r => r.IsExcused);
        decimal absencePercentage = totalDays > 0 ? (decimal)absentDays / totalDays * 100m : 0;

        decimal baseRequiredAmount = 0;
        if (currentEnrollment?.ClassRoom != null)
        {
            var stageFee = await _context.StageFees.FirstOrDefaultAsync(s => s.StageName == currentEnrollment.ClassRoom.Stage);
            if (stageFee != null) baseRequiredAmount = stageFee.FeeAmount;
        }

        decimal discount = student.HasHalfDiscount ? (baseRequiredAmount / 2) : 0;
        decimal finalRequired = baseRequiredAmount - discount;
        
        // Use student's AmountPaid instead of SubscriptionPayments sum, as FinancialController relies on AmountPaid directly
        decimal paidAmount = student.AmountPaid; 
        decimal debt = finalRequired - paidAmount;
        
        var gradesList = stGrades.Select(g => new { 
            subject = g.SubjectName, 
            term = g.Term,
            academicYear = g.AcademicYear,
            examScore = g.ExamScore,
            attendanceScore = g.AttendanceScore,
            totalScore = g.TotalScore
        }).OrderByDescending(g => g.academicYear).ThenBy(g => g.term).ThenBy(g => g.subject).ToList();

        return Ok(new {
            success = true,
            data = new {
                id = student.Id,
                name = student.Name,
                code = $"ST-{student.Id}",
                className = currentEnrollment?.ClassRoom?.Name ?? "غير مسجل",
                parentName = student.Mother?.Name ?? "غير مسجل",
                phone = student.Mother?.Phone ?? "غير مسجل",
                
                attendance = new {
                    totalDays, presentDays, absentDays, excusedDays, percentage = Math.Round(absencePercentage, 1)
                },
                
                financial = new {
                    required = baseRequiredAmount, paid = paidAmount, discount, debt
                },

                grades = gradesList
            }
        });
    }
}
