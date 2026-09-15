namespace SchoolSystemAPI.Models;

public class FcmToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // e.g. iOS, Android, Web
    public DateTime Created { get; set; }

    public int AppUserId { get; set; }
    public AppUser? AppUser { get; set; }
}
