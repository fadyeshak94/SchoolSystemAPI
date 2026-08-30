using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using System.Text.Json;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class SiblingsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public SiblingsController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions()
    {
        // 1. Get all students without a family ID
        var newStudents = (await _uow.Students.FindAsync(s => string.IsNullOrEmpty(s.FamilyId))).ToList();
        
        // We also need students WITH family ID to check if new students can join them
        var linkedStudents = (await _uow.Students.FindAsync(s => !string.IsNullOrEmpty(s.FamilyId))).ToList();
        
        var suggestions = new List<object>();
        var groupedStudents = new HashSet<int>();

        // 2. Group by phone numbers
        var phoneGroups = new Dictionary<string, List<Student>>();
        foreach (var student in newStudents.Concat(linkedStudents))
        {
            if (!string.IsNullOrWhiteSpace(student.PhonesJson) && student.PhonesJson != "[]")
            {
                try
                {
                    var phones = JsonSerializer.Deserialize<List<string>>(student.PhonesJson) ?? new List<string>();
                    foreach (var phone in phones)
                    {
                        var normalized = phone.Trim();
                        if (normalized.Length > 0)
                        {
                            if (!phoneGroups.ContainsKey(normalized))
                            {
                                phoneGroups[normalized] = new List<Student>();
                            }
                            phoneGroups[normalized].Add(student);
                        }
                    }
                }
                catch { }
            }
        }

        // Add suggestions based on shared phone
        foreach (var kvp in phoneGroups)
        {
            var distinctStudents = kvp.Value.DistinctBy(s => s.Id).ToList();
            
            // Only suggest if at least ONE new student is in the group
            var newInGroup = distinctStudents.Where(s => string.IsNullOrEmpty(s.FamilyId)).ToList();
            if (distinctStudents.Count > 1 && newInGroup.Any())
            {
                var ids = distinctStudents.Select(s => s.Id).ToList();
                if (!ids.All(id => groupedStudents.Contains(id)))
                {
                    // Check if there's an existing family
                    var existingFamilyId = distinctStudents.FirstOrDefault(s => !string.IsNullOrEmpty(s.FamilyId))?.FamilyId;

                    suggestions.Add(new
                    {
                        type = "PhoneMatch",
                        reason = $"رقم هاتف مشترك ({kvp.Key})",
                        isExistingFamily = existingFamilyId != null,
                        existingFamilyId = existingFamilyId,
                        students = distinctStudents.Select(s => new { s.Id, s.Name, s.GovGrade, s.FamilyId })
                    });
                    foreach (var id in ids) groupedStudents.Add(id);
                }
            }
        }

        // 3. Group by Father's name (2nd and 3rd words)
        var nameGroups = new Dictionary<string, List<Student>>();
        foreach (var student in newStudents.Concat(linkedStudents))
        {
            if (groupedStudents.Contains(student.Id)) continue; // skip already grouped

            var parts = student.Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                // Father's name usually part 2 and 3
                string fatherName = parts[1] + " " + parts[2];
                if (!nameGroups.ContainsKey(fatherName))
                {
                    nameGroups[fatherName] = new List<Student>();
                }
                nameGroups[fatherName].Add(student);
            }
            else if (parts.Length == 2)
            {
                 string fatherName = parts[1];
                 if (!nameGroups.ContainsKey(fatherName))
                 {
                     nameGroups[fatherName] = new List<Student>();
                 }
                 nameGroups[fatherName].Add(student);
            }
        }

        foreach (var kvp in nameGroups)
        {
            var distinctStudents = kvp.Value.DistinctBy(s => s.Id).ToList();
            var newInGroup = distinctStudents.Where(s => string.IsNullOrEmpty(s.FamilyId)).ToList();
            
            if (distinctStudents.Count > 1 && newInGroup.Any())
            {
                var existingFamilyId = distinctStudents.FirstOrDefault(s => !string.IsNullOrEmpty(s.FamilyId))?.FamilyId;
                suggestions.Add(new
                {
                    type = "NameMatch",
                    reason = $"اسم أب مشترك ({kvp.Key})",
                    isExistingFamily = existingFamilyId != null,
                    existingFamilyId = existingFamilyId,
                    students = distinctStudents.Select(s => new { s.Id, s.Name, s.GovGrade, s.FamilyId })
                });
            }
        }

        return Ok(new { success = true, suggestions });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmSiblings([FromBody] ConfirmSiblingsDto dto)
    {
        if (dto.StudentIds == null || dto.StudentIds.Count < 2)
            return BadRequest(new { success = false, message = "يجب تحديد طالبين على الأقل لربطهم كإخوة" });

        string familyIdToUse;

        if (!string.IsNullOrEmpty(dto.ExistingFamilyId))
        {
            familyIdToUse = dto.ExistingFamilyId;
        }
        else
        {
            // Create new family
            familyIdToUse = Guid.NewGuid().ToString();
            var newFamily = new Family { Id = familyIdToUse };
            await _uow.Families.AddAsync(newFamily);
        }

        foreach (var id in dto.StudentIds)
        {
            var student = await _uow.Students.GetByIdAsync(id);
            if (student != null && string.IsNullOrEmpty(student.FamilyId))
            {
                student.FamilyId = familyIdToUse;
                _uow.Students.Update(student);
            }
        }

        await _uow.CompleteAsync();
        return Ok(new { success = true, message = "تم ربط الطلاب كإخوة بنجاح" });
    }

    [HttpGet("confirmed")]
    public async Task<IActionResult> GetConfirmedSiblings()
    {
        var families = (await _uow.Families.FindAsync(f => true)).ToList();
        var students = (await _uow.Students.FindAsync(s => !string.IsNullOrEmpty(s.FamilyId))).ToList();

        var result = families.Select(f => new
        {
            familyId = f.Id,
            address = f.Address,
            fatherPhone = f.FatherPhone,
            motherPhone = f.MotherPhone,
            students = students.Where(s => s.FamilyId == f.Id).Select(s => new { s.Id, s.Name, s.GovGrade }).ToList()
        }).Where(f => f.students.Any()).ToList(); // only return families with students

        return Ok(new { success = true, families = result });
    }

    [HttpPut("family/{id}")]
    public async Task<IActionResult> UpdateFamilyDetails(string id, [FromBody] UpdateFamilyDto dto)
    {
        var familyResult = await _uow.Families.FindAsync(f => f.Id == id);
        var family = familyResult.FirstOrDefault();
        if (family == null) return NotFound(new { success = false, message = "لم يتم العثور على الأسرة" });

        family.Address = dto.Address ?? "";
        family.FatherPhone = dto.FatherPhone ?? "";
        family.MotherPhone = dto.MotherPhone ?? "";

        _uow.Families.Update(family);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم تحديث بيانات الأسرة بنجاح" });
    }
}

public class ConfirmSiblingsDto
{
    public List<int> StudentIds { get; set; } = new List<int>();
    public string? ExistingFamilyId { get; set; }
}

public class UpdateFamilyDto
{
    public string? Address { get; set; }
    public string? FatherPhone { get; set; }
    public string? MotherPhone { get; set; }
}
