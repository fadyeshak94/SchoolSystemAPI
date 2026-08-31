using MediatR;
using Microsoft.AspNetCore.SignalR;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Hubs;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Features.Notifications;

public class SendNotificationCommand : IRequest<bool>
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "System";
    public int? RelatedEntityId { get; set; }
}

public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public SendNotificationCommandHandler(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<bool> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        // 1. Save to database
        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            RelatedEntityId = request.RelatedEntityId,
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Send via SignalR
        await _hubContext.Clients.Group(request.UserId.ToString())
            .SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.CreatedAt,
                notification.RelatedEntityId
            }, cancellationToken);

        return true;
    }
}
