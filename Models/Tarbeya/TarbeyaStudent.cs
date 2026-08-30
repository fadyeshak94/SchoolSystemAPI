namespace SchoolSystemAPI.Models;

public class TarbeyaStudent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Area { get; set; } 
    public string? GeneralNotes { get; set; }
    public string? PrivateNotes { get; set; }
    
    public int ClassId { get; set; }
    public TarbeyaClass? Class { get; set; }
    
    public ICollection<TarbeyaAttendance> Attendances { get; set; } = new List<TarbeyaAttendance>();
}
