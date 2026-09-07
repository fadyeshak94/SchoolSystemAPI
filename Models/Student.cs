namespace SchoolSystemAPI.Models;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; 
    public string Gender { get; set; } = string.Empty; // النوع
    public bool IsDeacon { get; set; } // مرشوم
    public string GovGrade { get; set; } = string.Empty; // السنة الحكومية
    public string PhonesJson { get; set; } = "[]"; // الهواتف كـ JSON
    public decimal AmountPaid { get; set; } // المصروفات المدفوعة
    public bool HasHalfDiscount { get; set; } // خصم 50%
    
    public ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();
    
    public string? FamilyId { get; set; } // لمعرفة الأخوة
    public Family? Family { get; set; }
    
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<StudentGrade> Grades { get; set; } = new List<StudentGrade>();
}
