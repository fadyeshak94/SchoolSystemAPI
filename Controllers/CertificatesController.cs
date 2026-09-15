using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Services;
using System.IO;

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
    public async Task<IActionResult> GenerateClassCertificates(int classId, [FromQuery] string? academicYear)
    {
        var classRoom = await _uow.ClassRooms.GetByIdAsync(classId);
        if (classRoom == null) return NotFound(new { success = false, message = "الفصل غير موجود" });

        var students = await _uow.Students.GetQueryable().Where(s => s.Enrollments.Any(e => e.ClassRoomId == classId && (string.IsNullOrEmpty(academicYear) || e.AcademicYear == academicYear))).ToListAsync();
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
        
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var urlHelper = HttpContext.RequestServices.GetRequiredService<IUrlHelperService>();
        var dirPath = Path.Combine(env.WebRootPath, "generated");
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
        
        var filename = $"certificates_{classRoom.Id}_{DateTime.Now.Ticks}.pdf";
        var filePath = Path.Combine(dirPath, filename);
        await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);
        
        var url = urlHelper.GetAbsoluteUrl($"/generated/{filename}");

        return Ok(new 
        { 
            success = true, 
            url = url, 
            filename = filename,
            count = passingStudents.Count 
        });
    }

    [HttpGet("student/{studentId}/idcard")]
    public async Task<IActionResult> GenerateStudentIdCard(int studentId)
    {
        var student = await _uow.Students.GetQueryable().Include(s => s.Enrollments).FirstOrDefaultAsync(s => s.Id == studentId);
        if (student == null) return NotFound(new { success = false, message = "الطالب غير موجود" });

        var cId = student.Enrollments?.OrderByDescending(e => e.AcademicYear).FirstOrDefault()?.ClassRoomId ?? 0;
        var classRoom = await _uow.ClassRooms.GetByIdAsync(cId);
        // student.ClassRoom = classRoom; // Removed because ClassRoom property no longer exists

        var settings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        string academicYear = settings?.AcademicYear ?? "2026-2027";

        // This assumes IDocumentService is injected, we need to inject it in constructor.
        var documentService = HttpContext.RequestServices.GetRequiredService<IDocumentService>();
        var pngBytes = documentService.GenerateStudentIdCard(student, academicYear);
        
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var urlHelper = HttpContext.RequestServices.GetRequiredService<IUrlHelperService>();
        var dirPath = Path.Combine(env.WebRootPath, "generated");
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
        
        var filename = $"idcard_{student.Id}_{DateTime.Now.Ticks}.png";
        var filePath = Path.Combine(dirPath, filename);
        await System.IO.File.WriteAllBytesAsync(filePath, pngBytes);
        
        var url = urlHelper.GetAbsoluteUrl($"/generated/{filename}");

        return Ok(new 
        { 
            success = true, 
            url = url, 
            filename = filename
        });
    }

    [HttpGet("class/{classId}/idcards")]
    public async Task<IActionResult> GenerateClassIdCards(int classId, [FromQuery] string? academicYear)
    {
        var classRoom = await _uow.ClassRooms.GetByIdAsync(classId);
        if (classRoom == null) return NotFound(new { success = false, message = "الفصل غير موجود" });

        var students = await _uow.Students.GetQueryable().Where(s => s.Enrollments.Any(e => e.ClassRoomId == classId && (string.IsNullOrEmpty(academicYear) || e.AcademicYear == academicYear))).ToListAsync();
        if (!students.Any()) return BadRequest(new { success = false, message = "لا يوجد طلاب في هذا الفصل" });

        // foreach (var s in students) s.ClassRoom = classRoom;

        var settings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        string resolvedAcademicYear = !string.IsNullOrEmpty(academicYear) ? academicYear : (settings?.AcademicYear ?? "2026-2027");

        var documentService = HttpContext.RequestServices.GetRequiredService<IDocumentService>();
        var zipBytes = documentService.GenerateClassIdCardsZip(students, resolvedAcademicYear);
        
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var urlHelper = HttpContext.RequestServices.GetRequiredService<IUrlHelperService>();
        var dirPath = Path.Combine(env.WebRootPath, "generated");
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
        
        var filename = $"idcards_{classRoom.Id}_{DateTime.Now.Ticks}.zip";
        var filePath = Path.Combine(dirPath, filename);
        await System.IO.File.WriteAllBytesAsync(filePath, zipBytes);
        
        var url = urlHelper.GetAbsoluteUrl($"/generated/{filename}");

        return Ok(new 
        { 
            success = true, 
            url = url, 
            filename = filename,
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
