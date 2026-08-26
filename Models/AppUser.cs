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
    public bool PendingReset { get; set; } = false;

    public int? ClassRoomId { get; set; }
    public ClassRoom? ClassRoom { get; set; }
}
