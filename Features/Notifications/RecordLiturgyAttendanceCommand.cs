using MediatR;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Features.Notifications;

public class RecordLiturgyAttendanceCommand : IRequest<bool>
{
    public int StudentId { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Attended";
}

public class RecordLiturgyAttendanceCommandHandler : IRequestHandler<RecordLiturgyAttendanceCommand, bool>
{
    private readonly ApplicationDbContext _context;
    public RecordLiturgyAttendanceCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RecordLiturgyAttendanceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.TarbeyaLiturgyAttendances
            .FirstOrDefaultAsync(a => a.StudentId == request.StudentId && a.Date == request.Date.Date, cancellationToken);

        if (existing != null)
        {
            existing.Status = request.Status;
        }
        else
        {
            _context.TarbeyaLiturgyAttendances.Add(new TarbeyaLiturgyAttendance
            {
                StudentId = request.StudentId,
                Date = request.Date.Date,
                Status = request.Status
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
