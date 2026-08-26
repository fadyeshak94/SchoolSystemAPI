namespace SchoolSystemAPI.Models;

public class StudentGrade
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public string Term { get; set; } = string.Empty; // ت1 أو ت2
    public string SubjectName { get; set; } = string.Empty; // أجبية، الحان، طقس، الخ
    
    public decimal ExamScore { get; set; } // درجة الامتحان
    public decimal AttendanceScore { get; set; } // درجة الحضور
    public decimal TotalScore => ExamScore + AttendanceScore; 
}
