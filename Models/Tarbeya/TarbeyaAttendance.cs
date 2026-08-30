namespace SchoolSystemAPI.Models;

public enum TarbeyaAttendanceStatus
{
    Present,
    Absent,
    Excused
}

public class TarbeyaAttendance
{
    public int Id { get; set; }
    
    public int StudentId { get; set; }
    public TarbeyaStudent? Student { get; set; }
    
    public DateTime Date { get; set; }
    public TarbeyaAttendanceStatus Status { get; set; }
}
