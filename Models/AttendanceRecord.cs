namespace SchoolSystemAPI.Models;

public class AttendanceRecord
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty; // حاضر، غائب، الخ
    public string Term { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public bool IsExcused { get; set; } = false; // فلاج لتمييز حضور العذر
}
