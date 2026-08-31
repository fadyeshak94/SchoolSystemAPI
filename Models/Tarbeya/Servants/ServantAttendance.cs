namespace SchoolSystemAPI.Models;

public class ServantAttendance
{
    public int Id { get; set; }
    
    public int ServantId { get; set; }
    public AppUser? Servant { get; set; }

    public int FamilyId { get; set; }
    public TarbeyaFamily? Family { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;
    public string MeetingType { get; set; } = "PrepMeeting"; // PrepMeeting, Service, Liturgy
    public string Status { get; set; } = "Present"; // Present, Absent, Excused
}
