namespace SchoolSystemAPI.Models;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "System"; // TaskAssigned, MissingConfession, AbsenceAlert
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public int? RelatedEntityId { get; set; } // Reference to Task, Trip, etc.
}
