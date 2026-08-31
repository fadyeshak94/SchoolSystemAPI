namespace SchoolSystemAPI.Models;

public class TarbeyaLiturgyAttendance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public TarbeyaStudent? Student { get; set; }
    
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Attended"; // Attended, Absent
}
