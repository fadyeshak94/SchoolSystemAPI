using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Services;

public class DocumentService : IDocumentService
{
    public byte[] GenerateClassIdCardsZip(IEnumerable<Student> students, string academicYear)
    {
        return Array.Empty<byte>(); // Placeholder
    }

    public byte[] GenerateStudentIdCard(Student student, string academicYear)
    {
        return Array.Empty<byte>(); // Placeholder
    }
}
