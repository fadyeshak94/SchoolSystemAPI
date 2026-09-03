namespace SchoolSystemAPI.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = "User"; // Admin, User
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string? StageAccess { get; set; }
    public string? Title { get; set; } // مثل: الناظر (العضو المنتدب)، عضو مجلس إدارة، أمين مرحلة حضانة، مسؤولة سكرتارية، خادم
    public string? PhoneNumber { get; set; }
    public string? ConfessionFather { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool PendingReset { get; set; } = false;

    public int? ClassRoomId { get; set; }
    public ClassRoom? ClassRoom { get; set; }

    public ICollection<ServantAssignment> ServantAssignments { get; set; } = new List<ServantAssignment>();
}

