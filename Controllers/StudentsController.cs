using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> GetStudentsByClass(int classId)
    {
        // التحقق من الصلاحيات (هل اليوزر أدمن أو له صلاحية على الفصل ده؟)
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userClassId = User.FindFirst("ClassRoomId")?.Value;

        if (userRole != "Admin" && userClassId != classId.ToString())
        {
            return Forbid("ليس لديك صلاحية الوصول لهذا الفصل");
        }

        var students = await _uow.Students.FindAsync(s => s.ClassRoomId == classId);
        
        var result = students.Select(s => new
        {
            id = s.Id,
            name = s.Name,
            phone = s.PhonesJson, // ممكن نفك الـ JSON هنا لو الواجهة محتاجاه كـ Array
            gender = s.Gender,
            isDeacon = s.IsDeacon
        }).OrderBy(s => s.name).ToList();

        return Ok(new { students = result });
    }

    // 2. إضافة طالب جديد
    [HttpPost]
    public async Task<IActionResult> AddStudent([FromBody] AddStudentDto dto)
    {
        // التحقق من إن الـ ID مش مكرر
        var existingStudent = await _uow.Students.GetByIdAsync(dto.Id);
        if (existingStudent != null)
            return BadRequest(new { success = false, message = "رقم الطالب ده مستخدم بالفعل لطالب تاني" });

        var newStudent = new Student
        {
            Id = dto.Id,
            Name = dto.Name,
            ClassRoomId = dto.ClassId,
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
        var student = await _uow.Students.GetByIdAsync(id);
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
        
        if (dto.AmountPaid.HasValue)
            student.AmountPaid = dto.AmountPaid.Value;

        if (dto.ClassId.HasValue)
            student.ClassRoomId = dto.ClassId.Value;

        _uow.Students.Update(student);
        if (dto.AmountPaid.HasValue && dto.AmountPaid.Value > 0)
        {
            await _uow.SubscriptionPayments.AddAsync(new SubscriptionPayment {
                StudentId = student.Id,
                IsNewStudent = false,
                Amount = dto.AmountPaid.Value,
                PaymentDate = DateTime.UtcNow
            });
        }
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم حفظ بيانات الطالب بنجاح" });
    }

    // 4. البحث المتقدم عن طالب
    [HttpGet("search")]
    public async Task<IActionResult> SearchStudents([FromQuery] int? id, [FromQuery] string? name, [FromQuery] int? classId)
    {
        var studentsQuery = await _uow.Students.FindAsync(s => true);

        if (id.HasValue)
            studentsQuery = studentsQuery.Where(s => s.Id == id.Value);
        
        if (!string.IsNullOrWhiteSpace(name))
        {
            var normalizedQuery = NormalizeArabic(name);
            studentsQuery = studentsQuery.Where(s => NormalizeArabic(s.Name).Contains(normalizedQuery));
        }
            
        if (classId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.ClassRoomId == classId.Value);

        var classes = await _uow.ClassRooms.FindAsync(c => true);
        var classMap = classes.ToDictionary(c => c.Id, c => c);

        var result = studentsQuery.Select(s => new
        {
            id = s.Id,
            name = s.Name,
            className = classMap.ContainsKey(s.ClassRoomId) ? classMap[s.ClassRoomId].Name : "غير مسجل",
            stage = classMap.ContainsKey(s.ClassRoomId) ? classMap[s.ClassRoomId].Stage : "",
            phone = s.PhonesJson,
            govGrade = s.GovGrade,
            gender = s.Gender,
            isDeacon = s.IsDeacon
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

    // جلب بيانات طالب واحد بالكامل
    [HttpGet("{id}")]
    public async Task<IActionResult> GetStudentById(int id)
    {
        var student = await _uow.Students.GetByIdAsync(id);
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

        return Ok(new
        {
            id = student.Id,
            name = student.Name,
            classRoomId = student.ClassRoomId,
            gender = student.Gender,
            isDeacon = student.IsDeacon,
            govGrade = student.GovGrade,
            phone1 = phone1,
            phone2 = phone2
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
    [HttpDelete("{id}")]
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
}

