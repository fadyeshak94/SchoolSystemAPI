using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;

namespace SchoolSystemAPI.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> SendPushNotificationAsync(string deviceToken, string title, string body, object data = null)
    {
        // Placeholder for actual FCM integration
        // Example: FirebaseMessaging.DefaultInstance.SendAsync(...)
        _logger.LogInformation($"FCM Push Notification sent to token {deviceToken}. Title: {title}, Body: {body}");
        return await Task.FromResult(true);
    }

    public async Task<bool> SendToUserAsync(int userId, string title, string body, object data = null)
    {
        var fcmTokens = await _context.FcmTokens
            .Where(t => t.AppUserId == userId)
            .Select(t => t.Token)
            .ToListAsync();

        if (!fcmTokens.Any())
        {
            _logger.LogWarning($"No FCM tokens found for user {userId}");
            return false;
        }

        foreach (var token in fcmTokens)
        {
            await SendPushNotificationAsync(token, title, body, data);
        }

        return true;
    }
}
