using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Services;

public interface IDocumentService
{
    byte[] GenerateStudentIdCard(Student student, string academicYear);
    byte[] GenerateClassIdCardsZip(IEnumerable<Student> students, string academicYear);
}
