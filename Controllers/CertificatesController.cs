using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Services;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CertificatesController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IPdfService _pdfService;
    private readonly IResultsService _resultsService;

    public CertificatesController(IUnitOfWork uow, IPdfService pdfService, IResultsService resultsService)
    {
        _uow = uow;
        _pdfService = pdfService;
        _resultsService = resultsService;
    }

    [HttpGet("class/{classId}/generate")]
    public async Task<IActionResult> GenerateClassCertificates(int classId)
    {
        var classRoom = await _uow.ClassRooms.GetByIdAsync(classId);
        if (classRoom == null) return NotFound(new { success = false, message = "الفصل غير موجود" });

        var students = await _uow.Students.FindAsync(s => s.ClassRoomId == classId);
        var studentIds = students.Select(s => s.Id).ToList();

        var allGrades = await _uow.StudentGrades.FindAsync(g => studentIds.Contains(g.StudentId));

        var passingStudents = new List<StudentCertificateDto>();

        foreach (var student in students)
        {
            var studentGrades = allGrades.Where(g => g.StudentId == student.Id).ToList();
            var finalTotal = studentGrades.Sum(g => g.TotalScore);
            var percentage = _resultsService.CalculatePercentage(finalTotal, classRoom.Stage);

            // تصفية الناجحين فقط (50% فأكثر)
            if (_resultsService.IsPassing(percentage))
            {
                var subjectsDict = studentGrades
                    .GroupBy(g => g.SubjectName)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalScore));

                passingStudents.Add(new StudentCertificateDto
                {
                    Name = student.Name,
                    FinalTotal = finalTotal,
                    Percentage = percentage,
                    Tier = GetTier(percentage), // دالة مساعدة لتحديد التقدير (ممتاز، جيد جدا، الخ)
                    SubjectsGrades = subjectsDict
                });
            }
        }

        if (!passingStudents.Any())
            return BadRequest(new { success = false, message = "لا يوجد طلاب حاصلين على 50% أو أكثر في هذا الفصل" });

        var pdfBytes = _pdfService.GenerateCertificatesPdf(classRoom.Name, classRoom.Stage, classRoom.Year, passingStudents);
        var base64 = Convert.ToBase64String(pdfBytes);

        return Ok(new 
        { 
            success = true, 
            base64 = base64, 
            filename = $"شهادات_{classRoom.Name}.pdf",
            count = passingStudents.Count 
        });
    }

    [HttpGet("student/{studentId}/idcard")]
    public async Task<IActionResult> GenerateStudentIdCard(int studentId)
    {
        var student = await _uow.Students.GetByIdAsync(studentId);
        if (student == null) return NotFound(new { success = false, message = "الطالب غير موجود" });

        var classRoom = await _uow.ClassRooms.GetByIdAsync(student.ClassRoomId);
        student.ClassRoom = classRoom; // Ensure navigation property is populated for the service

        var settings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        string academicYear = settings?.AcademicYear ?? "2026-2027";

        // This assumes IDocumentService is injected, we need to inject it in constructor.
        var documentService = HttpContext.RequestServices.GetRequiredService<IDocumentService>();
        var pngBytes = documentService.GenerateStudentIdCard(student, academicYear);
        var base64 = Convert.ToBase64String(pngBytes);

        return Ok(new 
        { 
            success = true, 
            base64 = base64, 
            filename = $"بطاقة_{student.Name}.png"
        });
    }

    [HttpGet("class/{classId}/idcards")]
    public async Task<IActionResult> GenerateClassIdCards(int classId)
    {
        var classRoom = await _uow.ClassRooms.GetByIdAsync(classId);
        if (classRoom == null) return NotFound(new { success = false, message = "الفصل غير موجود" });

        var students = await _uow.Students.FindAsync(s => s.ClassRoomId == classId);
        if (!students.Any()) return BadRequest(new { success = false, message = "لا يوجد طلاب في هذا الفصل" });

        foreach (var s in students) s.ClassRoom = classRoom;

        var settings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        string academicYear = settings?.AcademicYear ?? "2026-2027";

        var documentService = HttpContext.RequestServices.GetRequiredService<IDocumentService>();
        var zipBytes = documentService.GenerateClassIdCardsZip(students, academicYear);
        var base64 = Convert.ToBase64String(zipBytes);

        return Ok(new 
        { 
            success = true, 
            base64 = base64, 
            filename = $"بطاقات_{classRoom.Name}.zip",
            count = students.Count()
        });
    }

    private string GetTier(decimal percentage)
    {
        if (percentage >= 85) return "ممتاز";
        if (percentage >= 75) return "جيد جدًا";
        if (percentage >= 65) return "جيد";
        return "مقبول";
    }
}

public class StudentCertificateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal FinalTotal { get; set; }
    public decimal Percentage { get; set; }
    public string Tier { get; set; } = string.Empty;
    public Dictionary<string, decimal> SubjectsGrades { get; set; } = new();
}
