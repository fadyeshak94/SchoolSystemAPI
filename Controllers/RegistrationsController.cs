using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RegistrationsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public RegistrationsController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestRegistration([FromBody] RegistrationRequestDto dto)
    {
        if (dto.IsRenewal && dto.Id.HasValue)
        {
            var existingPending = await _uow.PendingRegistrations.FindAsync(p => p.StudentId == dto.Id.Value && (p.Status == "Pending" || p.Status == "Waitlisted"));
            if (existingPending.Any())
            {
                return BadRequest(new { success = false, message = "هناك طلب تجديد معلق بالفعل لهذا الطالب، يرجى انتظار موافقة الإدارة." });
            }
        }
        else
        {
            var existingPending = await _uow.PendingRegistrations.FindAsync(p => p.Name == dto.Name && (p.Status == "Pending" || p.Status == "Waitlisted"));
            if (existingPending.Any())
            {
                return BadRequest(new { success = false, message = "يوجد طلب تسجيل معلق أو في قائمة الانتظار بنفس الاسم. لا يمكن تكرار الطلب." });
            }
        }
        var pending = new PendingRegistration
        {
            StudentId = dto.Id,
            Name = dto.Name,
            Gender = dto.Gender,
            IsDeacon = dto.IsDeacon,
            GovGrade = dto.GovGrade,
            PhonesJson = dto.PhonesJson,
            ClassId = dto.ClassId,
            AmountPaid = dto.AmountPaid,
            IsRenewal = dto.IsRenewal,
            Status = "Pending",
            RequestDate = DateTime.UtcNow
        };

        await _uow.PendingRegistrations.AddAsync(pending);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم تقديم الطلب بنجاح وهو في انتظار موافقة الإدارة." });
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingRegistrations()
    {
        var pendingList = await _uow.PendingRegistrations.FindAsync(p => p.Status == "Pending" || p.Status == "Waitlisted");
        var classRooms = await _uow.ClassRooms.FindAsync(c => true);
        var classMap = classRooms.ToDictionary(c => c.Id, c => c.Name);

        var result = pendingList.Select(p => new
        {
            p.Id,
            p.StudentId,
            p.Name,
            p.Gender,
            p.GovGrade,
            p.ClassId,
            ClassName = classMap.ContainsKey(p.ClassId) ? classMap[p.ClassId] : "غير معروف",
            p.AmountPaid,
            p.IsRenewal,
            p.RequestDate,
            p.Status
        }).OrderBy(p => p.RequestDate).ToList();

        return Ok(new { success = true, requests = result });
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveRegistration(int id, [FromBody] ApproveRequestDto dto)
    {
        var pending = await _uow.PendingRegistrations.GetByIdAsync(id);
        if (pending == null || (pending.Status != "Pending" && pending.Status != "Waitlisted"))
            return NotFound(new { success = false, message = "الطلب غير موجود أو تمت معالجته مسبقاً" });

        // Update class if admin changed it
        if (dto.ClassId.HasValue)
        {
            pending.ClassId = dto.ClassId.Value;
        }

        // Check capacity
        var studentsInClass = await _uow.Students.FindAsync(s => s.ClassRoomId == pending.ClassId);
        if (studentsInClass.Count() >= 35)
        {
            pending.Status = "Waitlisted";
            _uow.PendingRegistrations.Update(pending);
            await _uow.CompleteAsync();
            return BadRequest(new { success = false, message = "الفصل مكتمل العدد (35 طالب أو أكثر). تم تحويل الطلب إلى قائمة الانتظار (Waitlist)." });
        }

        Student student;

        if (pending.IsRenewal && pending.StudentId.HasValue)
        {
            student = await _uow.Students.GetByIdAsync(pending.StudentId.Value);
            if (student == null)
            {
                return BadRequest(new { success = false, message = "الطالب الأصلي غير موجود" });
            }
            // Update properties
            student.Name = pending.Name;
            student.Gender = pending.Gender;
            student.IsDeacon = pending.IsDeacon;
            student.GovGrade = pending.GovGrade;
            student.ClassRoomId = pending.ClassId;
            if (!string.IsNullOrWhiteSpace(pending.PhonesJson) && pending.PhonesJson != "[]")
                student.PhonesJson = pending.PhonesJson;

            student.AmountPaid = pending.AmountPaid;

            _uow.Students.Update(student);
        }
        else
        {
            // New student
            int finalStudentId = pending.StudentId ?? 0;
            if (finalStudentId == 0)
            {
                var allStudents = await _uow.Students.FindAsync(s => true);
                finalStudentId = allStudents.Any() ? allStudents.Max(s => s.Id) + 1 : 1;
            }
            else
            {
                // Check if ID is already taken
                var existing = await _uow.Students.GetByIdAsync(finalStudentId);
                if (existing != null)
                {
                    var allStudents = await _uow.Students.FindAsync(s => true);
                    finalStudentId = allStudents.Any() ? allStudents.Max(s => s.Id) + 1 : 1;
                }
            }

            student = new Student
            {
                Id = finalStudentId,
                Name = pending.Name,
                Gender = pending.Gender,
                IsDeacon = pending.IsDeacon,
                GovGrade = pending.GovGrade,
                ClassRoomId = pending.ClassId,
                PhonesJson = pending.PhonesJson,
                AmountPaid = pending.AmountPaid
            };

            await _uow.Students.AddAsync(student);
        }

        if (pending.AmountPaid > 0)
        {
            await _uow.SubscriptionPayments.AddAsync(new SubscriptionPayment
            {
                StudentId = student.Id,
                IsNewStudent = !pending.IsRenewal,
                Amount = pending.AmountPaid,
                PaymentDate = DateTime.UtcNow
            });
        }

        pending.Status = "Approved";
        _uow.PendingRegistrations.Update(pending);

        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تمت الموافقة على الطلب وحفظ البيانات بنجاح" });
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectRegistration(int id)
    {
        var pending = await _uow.PendingRegistrations.GetByIdAsync(id);
        if (pending == null || (pending.Status != "Pending" && pending.Status != "Waitlisted"))
            return NotFound(new { success = false, message = "الطلب غير موجود أو تمت معالجته مسبقاً" });

        pending.Status = "Rejected";
        _uow.PendingRegistrations.Update(pending);

        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم رفض الطلب بنجاح" });
    }

    [HttpPost("approve-bulk")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveBulkRegistrations([FromBody] BulkApproveRequestDto dto)
    {
        if (dto.Ids == null || !dto.Ids.Any())
            return BadRequest(new { success = false, message = "لم يتم تحديد أي طلبات للموافقة عليها" });

        int approvedCount = 0;
        int waitlistedCount = 0;

        foreach (var id in dto.Ids)
        {
            // Call the same logic, but we must do it manually here to avoid HTTP overhead
            // We can just reuse the logic, but for simplicity, let's copy the core logic
            var pending = await _uow.PendingRegistrations.GetByIdAsync(id);
            if (pending == null || (pending.Status != "Pending" && pending.Status != "Waitlisted"))
                continue;

            var studentsInClass = await _uow.Students.FindAsync(s => s.ClassRoomId == pending.ClassId);
            if (studentsInClass.Count() >= 35)
            {
                pending.Status = "Waitlisted";
                _uow.PendingRegistrations.Update(pending);
                waitlistedCount++;
                continue;
            }

            Student student;
            if (pending.IsRenewal && pending.StudentId.HasValue)
            {
                student = await _uow.Students.GetByIdAsync(pending.StudentId.Value);
                if (student != null)
                {
                    student.Name = pending.Name;
                    student.Gender = pending.Gender;
                    student.IsDeacon = pending.IsDeacon;
                    student.GovGrade = pending.GovGrade;
                    student.ClassRoomId = pending.ClassId;
                    if (!string.IsNullOrWhiteSpace(pending.PhonesJson) && pending.PhonesJson != "[]")
                        student.PhonesJson = pending.PhonesJson;
                    student.AmountPaid = pending.AmountPaid;
                    _uow.Students.Update(student);
                }
                else
                {
                    continue;
                }
            }
            else
            {
                int finalStudentId = pending.StudentId ?? 0;
                if (finalStudentId == 0)
                {
                    var allStudents = await _uow.Students.FindAsync(s => true);
                    finalStudentId = allStudents.Any() ? allStudents.Max(s => s.Id) + 1 : 1;
                }
                else
                {
                    var existing = await _uow.Students.GetByIdAsync(finalStudentId);
                    if (existing != null)
                    {
                        var allStudents = await _uow.Students.FindAsync(s => true);
                        finalStudentId = allStudents.Any() ? allStudents.Max(s => s.Id) + 1 : 1;
                    }
                }
                student = new Student
                {
                    Id = finalStudentId,
                    Name = pending.Name,
                    Gender = pending.Gender,
                    IsDeacon = pending.IsDeacon,
                    GovGrade = pending.GovGrade,
                    ClassRoomId = pending.ClassId,
                    PhonesJson = pending.PhonesJson,
                    AmountPaid = pending.AmountPaid
                };
                await _uow.Students.AddAsync(student);
            }

            if (pending.AmountPaid > 0)
            {
                await _uow.SubscriptionPayments.AddAsync(new SubscriptionPayment
                {
                    StudentId = student.Id,
                    IsNewStudent = !pending.IsRenewal,
                    Amount = pending.AmountPaid,
                    PaymentDate = DateTime.UtcNow
                });
            }

            pending.Status = "Approved";
            _uow.PendingRegistrations.Update(pending);
            
            // Note: Saving inside the loop because we need the student count and IDs to be fresh
            await _uow.CompleteAsync();
            approvedCount++;
        }

        return Ok(new { success = true, message = $"تم الموافقة على {approvedCount} طلب، وتم تحويل {waitlistedCount} إلى قائمة الانتظار لعدم وجود أماكن." });
    }
}

public class RegistrationRequestDto
{
    public int? Id { get; set; }
    public string Name { get; set; }
    public string Gender { get; set; }
    public bool IsDeacon { get; set; }
    public string GovGrade { get; set; }
    public string PhonesJson { get; set; }
    public int ClassId { get; set; }
    public decimal AmountPaid { get; set; }
    public bool IsRenewal { get; set; }
}

public class ApproveRequestDto
{
    public int? ClassId { get; set; }
}

public class BulkApproveRequestDto
{
    public List<int> Ids { get; set; } = new();
}
