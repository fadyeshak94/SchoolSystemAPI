using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using System.Security.Claims;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public StudentsController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // 1. جلب طلاب فصل معين
    [HttpGet("class/{classId}")]
    public async Task<IActionResult> GetStudentsByClass(int classId, [FromQuery] string? academicYear)
    {
        // التحقق من الصلاحيات (هل اليوزر أدمن أو له صلاحية على الفصل ده؟)
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userClassId = User.FindFirst("ClassRoomId")?.Value;

        if (userRole != "Admin" && userRole != "HeadSecretary" && userClassId != classId.ToString())
        {
            return Forbid("ليس لديك صلاحية الوصول لهذا الفصل");
        }

        var appSettings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        var resolvedYear = !string.IsNullOrEmpty(academicYear) ? academicYear : (appSettings?.AcademicYear ?? "2024/2025");

        var classRoom = await _uow.ClassRooms.GetByIdAsync(classId);
        if (classRoom == null) return NotFound("الفصل غير موجود");

        var stageFee = (await _uow.StageFees.FindAsync(f => f.StageName == classRoom.Stage)).FirstOrDefault();
        decimal baseFee = stageFee?.FeeAmount ?? 0m;

        var students = await _uow.Students.GetQueryable()
            .Where(s => s.Enrollments.Any(e => e.ClassRoomId == classId && e.AcademicYear == resolvedYear))
            .ToListAsync();
        
        var result = students.Select(s => 
        {
            decimal requiredFee = s.HasHalfDiscount ? baseFee / 2m : baseFee;
            decimal remainingAmount = requiredFee - s.AmountPaid;
            if (remainingAmount < 0) remainingAmount = 0;
            
            return new
            {
                id = s.Id,
                name = s.Name,
                phone = s.PhonesJson,
                gender = s.Gender,
                isDeacon = s.IsDeacon,
                govGrade = s.GovGrade,
                amountPaid = s.AmountPaid,
                amountRemaining = remainingAmount
            };
        }).OrderBy(s => s.name).ToList();

        return Ok(new { students = result });
    }

    // 2. إضافة طالب جديد
    [HttpPost]
    public async Task<IActionResult> AddStudent([FromBody] AddStudentDto dto)
    {
        // التحقق من إن الـ ID مش مكرر (مثلاً بسبب فتح الصفحة في نفس الوقت)
        var existingStudent = await _uow.Students.GetByIdAsync(dto.Id);
        if (existingStudent != null)
        {
            // نجيب أعلى ID موجود ونزود 1 عشان نحل مشكلة التكرار
            var allStudents = await _uow.Students.FindAsync(s => true);
            dto.Id = allStudents.Any() ? allStudents.Max(s => s.Id) + 1 : 1;
        }

        var newStudent = new Student
        {
            Id = dto.Id,
            Name = dto.Name,
            Enrollments = new List<StudentEnrollment> { new StudentEnrollment { ClassRoomId = dto.ClassId, AcademicYear = "2024/2025" } },
            Gender = dto.Gender,
            IsDeacon = dto.IsDeacon,
            PhonesJson = dto.PhonesJson,
            GovGrade = dto.GovGrade,
            AmountPaid = dto.AmountPaid
        };

        await _uow.Students.AddAsync(newStudent);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = $"تمت إضافة الطالب {newStudent.Name} بنجاح" });
    }

    // 3. تحديث بيانات طالب
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto)
    {
        var student = await _uow.Students.GetQueryable()
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (student == null)
            return NotFound(new { success = false, message = "الطالب غير موجود" });

        // تحديث البيانات
        student.Name = dto.Name ?? student.Name;
        student.Gender = dto.Gender ?? student.Gender;
        student.IsDeacon = dto.IsDeacon ?? student.IsDeacon;
        student.GovGrade = dto.GovGrade ?? student.GovGrade;
        
        var phonesList = new List<PhoneObj>();
        if (!string.IsNullOrWhiteSpace(dto.Phone1)) phonesList.Add(new PhoneObj { number = dto.Phone1, whatsapp = true });
        if (!string.IsNullOrWhiteSpace(dto.Phone2)) phonesList.Add(new PhoneObj { number = dto.Phone2, whatsapp = true });
        if (phonesList.Any()) 
            student.PhonesJson = System.Text.Json.JsonSerializer.Serialize(phonesList);
        
        decimal amountPaidByCash = 0;
        decimal amountWaived = dto.AmountWaived ?? 0;

        if (dto.AmountPaid.HasValue)
        {
            amountPaidByCash = dto.AmountPaid.Value - student.AmountPaid;
        }

        if (amountWaived > 0)
        {
            student.AmountPaid += amountWaived;
            await _uow.FinancialTransactions.AddAsync(new FinancialTransaction {
                Type = "WaivedFee",
                Description = $"إعفاء من المصاريف من قبل إدارة المدرسة للطالب: {student.Name}",
                Amount = amountWaived,
                TransactionDate = DateTime.UtcNow,
                AppUserId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int uid) ? uid : null
            });
        }

        if (amountPaidByCash > 0)
        {
            student.AmountPaid += amountPaidByCash;
            await _uow.SubscriptionPayments.AddAsync(new SubscriptionPayment {
                StudentId = student.Id,
                IsNewStudent = false,
                Amount = amountPaidByCash,
                PaymentDate = DateTime.UtcNow
            });
        }

        if (dto.ClassId.HasValue)
        {
            var enrollment = student.Enrollments?.OrderByDescending(e => e.AcademicYear).FirstOrDefault();
            if (enrollment != null) enrollment.ClassRoomId = dto.ClassId.Value;
            else 
            {
                if (student.Enrollments == null) student.Enrollments = new List<StudentEnrollment>();
                student.Enrollments.Add(new StudentEnrollment { ClassRoomId = dto.ClassId.Value, AcademicYear = "2024/2025" });
            }
        }

        _uow.Students.Update(student);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم حفظ بيانات الطالب بنجاح" });
    }

    // 4. البحث المتقدم عن طالب
    [HttpGet("search")]
    public async Task<IActionResult> SearchStudents([FromQuery] int? id, [FromQuery] string? name, [FromQuery] int? classId, [FromQuery] string? academicYear)
    {
        var studentsQuery = await _uow.Students.GetQueryable().Include(s => s.Enrollments).ToListAsync();
        var filteredStudents = studentsQuery.AsEnumerable();

        if (id.HasValue)
            filteredStudents = filteredStudents.Where(s => s.Id == id.Value);
        
        if (!string.IsNullOrWhiteSpace(name))
        {
            var normalizedQuery = NormalizeArabic(name);
            filteredStudents = filteredStudents.Where(s => NormalizeArabic(s.Name).Contains(normalizedQuery));
        }
            
        if (classId.HasValue)
            filteredStudents = filteredStudents.Where(s => s.Enrollments.Any(e => e.ClassRoomId == classId.Value && (string.IsNullOrEmpty(academicYear) || e.AcademicYear == academicYear)));

        var classes = await _uow.ClassRooms.FindAsync(c => true);
        var classMap = classes.ToDictionary(c => c.Id, c => c);

        var result = filteredStudents.Select(s => {
            var cId = string.IsNullOrEmpty(academicYear) 
                ? (s.Enrollments?.OrderByDescending(e => e.AcademicYear).FirstOrDefault()?.ClassRoomId ?? 0)
                : (s.Enrollments?.FirstOrDefault(e => e.AcademicYear == academicYear)?.ClassRoomId ?? 0);
            return new
            {
                id = s.Id,
                name = s.Name,
                className = classMap.ContainsKey(cId) ? classMap[cId].Name : "غير مسجل",
                stage = classMap.ContainsKey(cId) ? classMap[cId].Stage : "",
                phone = s.PhonesJson,
                govGrade = s.GovGrade,
                gender = s.Gender,
                isDeacon = s.IsDeacon
            };
        }).ToList();

        return Ok(new { success = true, students = result });
    }

    private string NormalizeArabic(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        return text.Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا")
                   .Replace("ة", "ه").Replace("ي", "ى")
                   .Trim();
    }

    // 5. الطلاب غير المسجلين في العام الجديد
    [HttpGet("unregistered")]
    public async Task<IActionResult> GetUnregisteredStudents([FromQuery] string? targetYear)
    {
        var appSettings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        var resolvedYear = !string.IsNullOrEmpty(targetYear) ? targetYear : (appSettings?.AcademicYear ?? "2024/2025");

        var allStudents = await _uow.Students.GetQueryable().Include(s => s.Enrollments).ToListAsync();
        var allClasses = await _uow.ClassRooms.FindAsync(c => true);
        var classMap = allClasses.ToDictionary(c => c.Id, c => c);
        var grades = await _uow.StudentGrades.FindAsync(g => true);

        var unregistered = allStudents
            .Where(s => s.Enrollments != null && s.Enrollments.Any() && !s.Enrollments.Any(e => e.AcademicYear == resolvedYear))
            .Select(s => {
                var lastEnrollment = s.Enrollments.OrderByDescending(e => e.AcademicYear).First();
                var stage = classMap.ContainsKey(lastEnrollment.ClassRoomId) ? classMap[lastEnrollment.ClassRoomId].Stage : "";
                
                var studentGrades = grades.Where(g => g.StudentId == s.Id).ToList();
                decimal totalScore = studentGrades.Sum(g => g.ExamScore + g.AttendanceScore);
                decimal maxScore = stage.Contains("ابتدائي") ? 400m : 500m;
                decimal percentage = maxScore > 0 ? (totalScore / maxScore) * 100m : 0;
                bool isPassed = percentage >= 50m;

                return new {
                    id = s.Id,
                    name = s.Name,
                    phone = s.PhonesJson,
                    lastYear = lastEnrollment.AcademicYear,
                    lastClassName = classMap.ContainsKey(lastEnrollment.ClassRoomId) ? classMap[lastEnrollment.ClassRoomId].Name : "غير مسجل",
                    lastStage = stage,
                    percentage = percentage,
                    isPassed = isPassed
                };
            })
            .OrderBy(s => s.lastStage).ThenBy(s => s.lastClassName).ThenBy(s => s.name)
            .ToList();

        return Ok(new { success = true, targetYear = resolvedYear, students = unregistered });
    }

    // جلب بيانات طالب واحد بالكامل
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetStudentById(int id)
    {
        var student = await _uow.Students.GetQueryable()
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (student == null) return NotFound(new { success = false, message = "الطالب غير موجود" });

        // Parse PhonesJson safely
        string phone1 = "";
        string phone2 = "";
        try {
            if(!string.IsNullOrEmpty(student.PhonesJson) && student.PhonesJson.StartsWith("[")) {
                if (student.PhonesJson.Contains("\"number\""))
                {
                    var arr = System.Text.Json.JsonSerializer.Deserialize<List<PhoneObj>>(student.PhonesJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if(arr != null && arr.Count > 0) phone1 = arr[0].number;
                    if(arr != null && arr.Count > 1) phone2 = arr[1].number;
                }
                else
                {
                    var arr = System.Text.Json.JsonSerializer.Deserialize<List<string>>(student.PhonesJson);
                    if(arr != null && arr.Count > 0) phone1 = arr[0];
                    if(arr != null && arr.Count > 1) phone2 = arr[1];
                }
            }
        } catch {}

        var classRoomId = student.Enrollments?.OrderByDescending(e => e.AcademicYear).FirstOrDefault()?.ClassRoomId ?? 0;
        decimal requiredFee = 0;
        if (classRoomId > 0)
        {
            var classRoom = await _uow.ClassRooms.GetByIdAsync(classRoomId);
            if (classRoom != null)
            {
                var stageFee = (await _uow.StageFees.FindAsync(f => f.StageName == classRoom.Stage)).FirstOrDefault();
                decimal baseFee = stageFee?.FeeAmount ?? 0m;
                requiredFee = student.HasHalfDiscount ? baseFee / 2m : baseFee;
            }
        }
        
        decimal amountRemaining = requiredFee - student.AmountPaid;
        if (amountRemaining < 0) amountRemaining = 0;

        return Ok(new
        {
            id = student.Id,
            name = student.Name,
            classRoomId = classRoomId,
            gender = student.Gender,
            isDeacon = student.IsDeacon,
            govGrade = student.GovGrade,
            phone1 = phone1,
            phone2 = phone2,
            amountPaid = student.AmountPaid,
            requiredFee = requiredFee,
            amountRemaining = amountRemaining
        });
    }

    // 5. اقتراح ID جديد
    [HttpGet("suggest-id")]
    public async Task<IActionResult> SuggestNextId()
    {
        var students = await _uow.Students.FindAsync(s => true);
        var nextId = students.Any() ? students.Max(s => s.Id) + 1 : 1;
        return Ok(new { nextId });
    }

    // 6. حذف طالب
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteStudent(int id)
    {
        var student = await _uow.Students.GetByIdAsync(id);
        if (student == null)
            return NotFound(new { success = false, message = "الطالب غير موجود" });

        _uow.Students.Remove(student);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم حذف الطالب بنجاح" });
    }

    [HttpGet("fix-phones")]
    public async Task<IActionResult> FixPhones()
    {
        var students = await _uow.Students.FindAsync(s => true);
        int updated = 0;
        foreach (var s in students)
        {
            if (string.IsNullOrWhiteSpace(s.PhonesJson) || s.PhonesJson == "[]") continue;
            try
            {
                // Try parsing as array of objects
                if (s.PhonesJson.Contains("\"number\""))
                {
                    var objs = System.Text.Json.JsonSerializer.Deserialize<List<PhoneObj>>(s.PhonesJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (objs != null)
                    {
                        foreach (var o in objs) o.whatsapp = true;
                        s.PhonesJson = System.Text.Json.JsonSerializer.Serialize(objs);
                        _uow.Students.Update(s);
                        updated++;
                    }
                }
                else
                {
                    // Try parsing as array of strings
                    var arr = System.Text.Json.JsonSerializer.Deserialize<List<string>>(s.PhonesJson);
                    if (arr != null && arr.Any())
                    {
                        var objs = arr.Select(str => new PhoneObj { number = str, whatsapp = true }).ToList();
                        s.PhonesJson = System.Text.Json.JsonSerializer.Serialize(objs);
                        _uow.Students.Update(s);
                        updated++;
                    }
                }
            }
            catch { }
        }
        await _uow.CompleteAsync();
        return Ok(new { success = true, updatedCount = updated });
    }

    [HttpGet("status-report")]
    public async Task<IActionResult> GetStudentsStatusReport([FromQuery] int? classId = null, [FromQuery] string? academicYear = null)
    {
        var studentsQuery = await _uow.Students.GetQueryable().Include(s => s.Enrollments).ToListAsync();
        var filteredStudents = studentsQuery.AsEnumerable();
        if (classId.HasValue)
        {
            filteredStudents = filteredStudents.Where(s => s.Enrollments.Any(e => e.ClassRoomId == classId.Value && (string.IsNullOrEmpty(academicYear) || e.AcademicYear == academicYear)));
        }

        var classes = await _uow.ClassRooms.FindAsync(c => true);
        var classMap = classes.ToDictionary(c => c.Id, c => c);

        var pendingRegistrations = await _uow.PendingRegistrations.FindAsync(p => true);
        var grades = await _uow.StudentGrades.FindAsync(g => true);

        var result = filteredStudents.Select(s =>
        {
            var cId = string.IsNullOrEmpty(academicYear) 
                ? (s.Enrollments?.OrderByDescending(e => e.AcademicYear).FirstOrDefault()?.ClassRoomId ?? 0)
                : (s.Enrollments?.FirstOrDefault(e => e.AcademicYear == academicYear)?.ClassRoomId ?? 0);
            var cRoom = classMap.ContainsKey(cId) ? classMap[cId] : null;
            var stage = cRoom?.Stage ?? "ابتدائي";
            
            var studentGrades = grades.Where(g => g.StudentId == s.Id).ToList();
            decimal totalScore = studentGrades.Sum(g => g.ExamScore + g.AttendanceScore);
            decimal maxScore = stage.Contains("ابتدائي") ? 400m : 500m;
            decimal percentage = maxScore > 0 ? (totalScore / maxScore) * 100m : 0;
            bool isPassed = percentage >= 50m;

            var renewalReq = pendingRegistrations.Where(p => p.StudentId == s.Id).OrderByDescending(p => p.RequestDate).FirstOrDefault();
            bool hasUpdatedData = renewalReq != null;
            string renewalStatus = renewalReq?.Status ?? "None";

            return new
            {
                id = s.Id,
                name = s.Name,
                className = cRoom?.Name ?? "غير مسجل",
                classId = cId,
                stage = stage,
                isPassed = isPassed,
                percentage = percentage,
                hasUpdatedData = hasUpdatedData,
                renewalStatus = renewalStatus
            };
        }).OrderBy(s => s.classId).ThenBy(s => s.name).ToList();

        return Ok(new { success = true, students = result });
    }
}

public class PhoneObj
{
    public string number { get; set; }
    public bool whatsapp { get; set; }
}

// الـ DTOs (Data Transfer Objects) لاستقبال البيانات من الواجهة
public class AddStudentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string Gender { get; set; } = string.Empty;
    public bool IsDeacon { get; set; }
    public string PhonesJson { get; set; } = "[]";
    public string GovGrade { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
}

public class UpdateStudentDto
{
    public string? Name { get; set; }
    public int? ClassId { get; set; }
    public string? Gender { get; set; }
    public bool? IsDeacon { get; set; }
    public string? GovGrade { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public decimal? AmountPaid { get; set; }
    public decimal? AmountWaived { get; set; }
}

