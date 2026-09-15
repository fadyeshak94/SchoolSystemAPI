namespace SchoolSystemAPI.Services;

public interface INotificationService
{
    Task<bool> SendPushNotificationAsync(string deviceToken, string title, string body, object data = null);
    Task<bool> SendToUserAsync(int userId, string title, string body, object data = null);
}
