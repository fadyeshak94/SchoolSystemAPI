using MediatR;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Features.Notifications;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Services;

public class TarbeyaNotificationWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TarbeyaNotificationWorker> _logger;

    public TarbeyaNotificationWorker(IServiceProvider services, ILogger<TarbeyaNotificationWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Tarbeya Notification Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var now = DateTime.Now;

                // 1. Task Due Alerts (Tasks due within 48 hours)
                var dueTasks = await context.ServiceTasks
                    .Where(t => t.Status != TarbeyaServiceTaskStatus.Completed && t.DueDate > now && t.DueDate <= now.AddDays(2))
                    .ToListAsync(stoppingToken);

                foreach (var task in dueTasks)
                {
                    // Avoid duplicate notifications (simple check)
                    bool hasNotified = await context.Notifications.AnyAsync(n => n.RelatedEntityId == task.Id && n.Type == "TaskAssigned", stoppingToken);
                    if (!hasNotified)
                    {
                        await mediator.Send(new SendNotificationCommand
                        {
                            UserId = task.AssignedToServantId,
                            Title = "تنبيه مهمة",
                            Message = $"المهمة '{task.Title}' اقترب موعد تسليمها.",
                            Type = "TaskAssigned",
                            RelatedEntityId = task.Id
                        }, stoppingToken);
                    }
                }

                // 2. Missing Confession Alerts (> 45 days)
                var cutoffDate = now.AddDays(-45);
                var studentsMissingConfession = await context.TarbeyaStudents
                    .Where(s => s.LastConfessionDate == null || s.LastConfessionDate < cutoffDate)
                    .Include(s => s.Class)
                    .ThenInclude(c => c.Stage)
                    .ToListAsync(stoppingToken);

                // Note: In real app, we'd send this to the servant responsible for the class.
                // For now, let's just log or send to Family Admin.

                _logger.LogInformation($"Processed notifications at {now}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in TarbeyaNotificationWorker.");
            }

            // Run once a day. For testing, you might want to change this to a smaller interval like Delay(TimeSpan.FromMinutes(1))
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
