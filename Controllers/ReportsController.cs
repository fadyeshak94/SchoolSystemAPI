using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,HeadSecretary,StageSupervisor,Secretary,Principal,BoardMember")]
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

    [Authorize(Roles = "Admin,HeadSecretary")]
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
    public async Task<IActionResult> GetAtRiskStudents(
        [FromQuery] string? academicYear, 
        [FromQuery] string? term, 
        [FromQuery] int consecutiveThreshold = 2, 
        [FromQuery] int totalAbsenceThreshold = 3)
    {
        var appSettings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        var resolvedYear = !string.IsNullOrEmpty(academicYear) ? academicYear : (appSettings?.AcademicYear ?? "2024/2025");
        var resolvedTerm = !string.IsNullOrEmpty(term) ? term : (appSettings?.CurrentTerm ?? "ت1");

        var normYear1 = resolvedYear.Replace("-", "/");
        var normYear2 = resolvedYear.Replace("/", "-");

        string altTerm = resolvedTerm;
        if (resolvedTerm == "ت1") altTerm = "1";
        else if (resolvedTerm == "ت2") altTerm = "2";
        else if (resolvedTerm == "1") altTerm = "ت1";
        else if (resolvedTerm == "2") altTerm = "ت2";

        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("Role")?.Value;
        var stageAccess = User.FindFirst("StageAccess")?.Value;
        var userClassId = User.FindFirst("ClassRoomId")?.Value;
        bool isTopManagement = userRole == "Admin" || userRole == "HeadSecretary" || userRole == "Principal" || userRole == "BoardMember";

        // Query students enrolled in the resolved academic year
        var studentsQuery = _uow.Students.GetQueryable()
            .Include(s => s.Mother)
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.ClassRoom)
            .Where(s => s.Enrollments.Any(e => e.AcademicYear == normYear1 || e.AcademicYear == normYear2));

        var students = await studentsQuery.ToListAsync();

        // Apply role permissions
        if (!isTopManagement)
        {
            if (userRole == "StageSupervisor" && !string.IsNullOrEmpty(stageAccess))
            {
                students = students.Where(s => s.Enrollments.Any(e => (e.AcademicYear == normYear1 || e.AcademicYear == normYear2) && e.ClassRoom != null && stageAccess.Contains(e.ClassRoom.Stage))).ToList();
            }
            else if ((userRole == "Secretary" || userRole == "User") && !string.IsNullOrEmpty(userClassId))
            {
                students = students.Where(s => s.Enrollments.Any(e => (e.AcademicYear == normYear1 || e.AcademicYear == normYear2) && e.ClassRoomId.ToString() == userClassId)).ToList();
            }
        }

        var studentIds = students.Select(s => s.Id).ToList();

        // Query attendance records for the resolved academic year and term
        var allRecords = studentIds.Any()
            ? await _uow.AttendanceRecords.FindAsync(a => 
                studentIds.Contains(a.StudentId) && 
                (a.AcademicYear == normYear1 || a.AcademicYear == normYear2) && 
                (a.Term == resolvedTerm || a.Term == altTerm))
            : new List<AttendanceRecord>();

        // Precompute distinct dates where attendance was recorded for each class
        var classDatesMap = new Dictionary<int, List<DateTime>>();
        var classIds = students.SelectMany(s => s.Enrollments)
            .Where(e => e.AcademicYear == normYear1 || e.AcademicYear == normYear2)
            .Select(e => e.ClassRoomId)
            .Distinct()
            .ToList();

        foreach (var cId in classIds)
        {
            var cStudentIds = students
                .Where(s => s.Enrollments.Any(e => e.ClassRoomId == cId && (e.AcademicYear == normYear1 || e.AcademicYear == normYear2)))
                .Select(s => s.Id)
                .ToHashSet();

            var cDates = allRecords
                .Where(r => cStudentIds.Contains(r.StudentId))
                .Select(r => r.Date.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            classDatesMap[cId] = cDates;
        }

        var result = new List<object>();

        foreach (var student in students)
        {
            var enrollment = student.Enrollments
                .OrderByDescending(e => e.Id)
                .FirstOrDefault(e => e.AcademicYear == normYear1 || e.AcademicYear == normYear2);

            if (enrollment == null) continue;

            var stRecords = allRecords.Where(r => r.StudentId == student.Id).ToList();

            // The dates where attendance was ACTUALLY taken for this student's class
            if (!classDatesMap.TryGetValue(enrollment.ClassRoomId, out var classDates) || classDates == null)
            {
                classDates = new List<DateTime>();
            }

            // Union with any dates the student specifically has records for, ordered chronologically
            var studentRecordedDates = classDates
                .Union(stRecords.Select(r => r.Date.Date))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (!studentRecordedDates.Any()) continue;

            int currentConsecutive = 0;
            int maxConsecutive = 0;
            var currentStreak = new List<DateTime>();
            var maxStreak = new List<DateTime>();
            var allStreaks = new List<List<string>>();
            var absentDates = new List<DateTime>();
            var excusedDates = new List<DateTime>();
            var presentDates = new List<DateTime>();

            foreach (var d in studentRecordedDates)
            {
                var rec = stRecords.FirstOrDefault(r => r.Date.Date == d);
                if (rec == null)
                {
                    // No record for this student on a date their class took attendance
                    if (currentConsecutive >= consecutiveThreshold)
                    {
                        allStreaks.Add(currentStreak.Select(x => x.ToString("yyyy-MM-dd")).ToList());
                    }
                    currentConsecutive = 0;
                    currentStreak.Clear();
                    continue;
                }

                if (rec.IsExcused)
                {
                    excusedDates.Add(d);
                    if (currentConsecutive >= consecutiveThreshold)
                    {
                        allStreaks.Add(currentStreak.Select(x => x.ToString("yyyy-MM-dd")).ToList());
                    }
                    currentConsecutive = 0;
                    currentStreak.Clear();
                }
                else if (rec.Status != null && rec.Status.Equals("Absent", StringComparison.OrdinalIgnoreCase))
                {
                    absentDates.Add(d);
                    currentConsecutive++;
                    currentStreak.Add(d);
                    if (currentConsecutive > maxConsecutive)
                    {
                        maxConsecutive = currentConsecutive;
                        maxStreak = new List<DateTime>(currentStreak);
                    }
                }
                else
                {
                    presentDates.Add(d);
                    if (currentConsecutive >= consecutiveThreshold)
                    {
                        allStreaks.Add(currentStreak.Select(x => x.ToString("yyyy-MM-dd")).ToList());
                    }
                    currentConsecutive = 0;
                    currentStreak.Clear();
                }
            }

            if (currentConsecutive >= consecutiveThreshold)
            {
                allStreaks.Add(currentStreak.Select(x => x.ToString("yyyy-MM-dd")).ToList());
            }

            bool isConsecutiveRisk = maxConsecutive >= consecutiveThreshold;
            bool isTotalAbsenceRisk = absentDates.Count > totalAbsenceThreshold;

            if (isConsecutiveRisk || isTotalAbsenceRisk)
            {
                string riskType;
                string riskLabel;

                if (isConsecutiveRisk && isTotalAbsenceRisk)
                {
                    riskType = "both";
                    riskLabel = $"خطر مرتفع (جمعتين متتاليتين + {absentDates.Count} غيابات)";
                }
                else if (isConsecutiveRisk)
                {
                    riskType = "consecutive";
                    riskLabel = $"غياب جمعتين متتاليتين ({maxConsecutive} جمع)";
                }
                else
                {
                    riskType = "total";
                    riskLabel = $"تجاوز حد الغياب ({absentDates.Count} جمع)";
                }

                // Extract primary phone and whatsapp
                string phone = student.Mother?.Phone ?? "";
                string whatsapp = student.Mother?.Whatsapp ?? "";

                if (!string.IsNullOrEmpty(student.PhonesJson) && student.PhonesJson.StartsWith("["))
                {
                    try
                    {
                        if (student.PhonesJson.Contains("\"number\""))
                        {
                            var arr = System.Text.Json.JsonSerializer.Deserialize<List<PhoneObj>>(student.PhonesJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (arr != null && arr.Any())
                            {
                                if (string.IsNullOrEmpty(phone)) phone = arr.FirstOrDefault()?.number ?? "";
                                if (string.IsNullOrEmpty(whatsapp)) whatsapp = arr.FirstOrDefault(p => p.whatsapp)?.number ?? arr.FirstOrDefault()?.number ?? "";
                            }
                        }
                        else
                        {
                            var arr = System.Text.Json.JsonSerializer.Deserialize<List<string>>(student.PhonesJson);
                            if (arr != null && arr.Any() && string.IsNullOrEmpty(phone))
                            {
                                phone = arr.FirstOrDefault() ?? "";
                            }
                        }
                    }
                    catch { }
                }
                if (string.IsNullOrEmpty(whatsapp)) whatsapp = phone;

                int totalRecorded = studentRecordedDates.Count;
                int absentCount = absentDates.Count;
                decimal absencePercentage = totalRecorded > 0 ? Math.Round((decimal)absentCount / totalRecorded * 100m, 1) : 0;

                result.Add(new
                {
                    studentId = student.Id,
                    studentCode = $"ST-{student.Id}",
                    studentName = student.Name,
                    classId = enrollment.ClassRoomId,
                    className = enrollment.ClassRoom?.Name ?? "غير مسجل",
                    stage = enrollment.ClassRoom?.Stage ?? "",
                    motherName = student.Mother?.Name ?? "",
                    phone = phone,
                    whatsapp = whatsapp,
                    totalRecordedFridays = totalRecorded,
                    presentCount = presentDates.Count,
                    absentCount = absentCount,
                    excusedCount = excusedDates.Count,
                    absencePercentage = absencePercentage,
                    maxConsecutiveAbsences = maxConsecutive,
                    consecutiveDates = maxStreak.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
                    allConsecutiveStreaks = allStreaks,
                    allAbsentDates = absentDates.OrderBy(d => d).Select(d => d.ToString("yyyy-MM-dd")).ToList(),
                    isConsecutiveRisk = isConsecutiveRisk,
                    isTotalAbsenceRisk = isTotalAbsenceRisk,
                    riskType = riskType,
                    riskLabel = riskLabel
                });
            }
        }

        var sorted = result
            .OrderByDescending(r => ((dynamic)r).isConsecutiveRisk && ((dynamic)r).isTotalAbsenceRisk)
            .ThenByDescending(r => ((dynamic)r).maxConsecutiveAbsences)
            .ThenByDescending(r => ((dynamic)r).absentCount)
            .ThenBy(r => ((dynamic)r).stage)
            .ThenBy(r => ((dynamic)r).className)
            .ThenBy(r => ((dynamic)r).studentName)
            .ToList();

        return Ok(new
        {
            success = true,
            academicYear = resolvedYear,
            term = resolvedTerm,
            totalAtRisk = sorted.Count,
            totalConsecutive = sorted.Count(r => ((dynamic)r).isConsecutiveRisk),
            totalHighAbsence = sorted.Count(r => ((dynamic)r).isTotalAbsenceRisk),
            totalBoth = sorted.Count(r => ((dynamic)r).isConsecutiveRisk && ((dynamic)r).isTotalAbsenceRisk),
            data = sorted
        });
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
