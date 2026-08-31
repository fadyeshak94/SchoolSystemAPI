using MediatR;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Features.Notifications;

public class RecordServantAttendanceCommand : IRequest<bool>
{
    public int ServantId { get; set; }
    public int FamilyId { get; set; }
    public DateTime Date { get; set; }
    public string MeetingType { get; set; } = "PrepMeeting";
    public string Status { get; set; } = "Present";
}

public class RecordServantAttendanceCommandHandler : IRequestHandler<RecordServantAttendanceCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly IMediator _mediator;

    public RecordServantAttendanceCommandHandler(ApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<bool> Handle(RecordServantAttendanceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.ServantAttendances
            .FirstOrDefaultAsync(a => a.ServantId == request.ServantId && a.FamilyId == request.FamilyId && a.Date == request.Date.Date && a.MeetingType == request.MeetingType, cancellationToken);

        if (existing != null)
        {
            existing.Status = request.Status;
        }
        else
        {
            _context.ServantAttendances.Add(new ServantAttendance
            {
                ServantId = request.ServantId,
                FamilyId = request.FamilyId,
                Date = request.Date.Date,
                MeetingType = request.MeetingType,
                Status = request.Status
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 3. Absence Alert: If missed PrepMeeting 2 times consecutively
        if (request.MeetingType == "PrepMeeting" && request.Status == "Absent")
        {
            var pastAbsences = await _context.ServantAttendances
                .Where(a => a.ServantId == request.ServantId && a.MeetingType == "PrepMeeting")
                .OrderByDescending(a => a.Date)
                .Take(2)
                .ToListAsync(cancellationToken);

            if (pastAbsences.Count == 2 && pastAbsences.All(a => a.Status == "Absent"))
            {
                // Find family admin
                var familyAdmins = await _context.Users.Where(u => u.Role == "TarbeyaFamilyAdmin" && u.TarbeyaFamilyId == request.FamilyId).ToListAsync(cancellationToken);
                var servant = await _context.Users.FindAsync(request.ServantId);

                foreach(var admin in familyAdmins)
                {
                    await _mediator.Send(new SendNotificationCommand
                    {
                        UserId = admin.Id,
                        Title = "تنبيه غياب متكرر",
                        Message = $"الخادم {servant?.Username} تغيب عن الاجتماع التحضيري لمرتين متتاليتين.",
                        Type = "AbsenceAlert"
                    }, cancellationToken);
                }
            }
        }

        return true;
    }
}
