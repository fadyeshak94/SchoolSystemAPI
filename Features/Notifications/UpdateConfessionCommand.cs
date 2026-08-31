using MediatR;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;

namespace SchoolSystemAPI.Features.Notifications;

public class UpdateConfessionCommand : IRequest<bool>
{
    public int StudentId { get; set; }
    public DateTime ConfessionDate { get; set; }
    public string? FatherConfessorName { get; set; }
}

public class UpdateConfessionCommandHandler : IRequestHandler<UpdateConfessionCommand, bool>
{
    private readonly ApplicationDbContext _context;
    public UpdateConfessionCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateConfessionCommand request, CancellationToken cancellationToken)
    {
        var student = await _context.TarbeyaStudents.FindAsync(request.StudentId);
        if (student == null) return false;

        student.LastConfessionDate = request.ConfessionDate;
        if (!string.IsNullOrEmpty(request.FatherConfessorName))
        {
            student.FatherConfessorName = request.FatherConfessorName;
            student.ConfessionFather = request.FatherConfessorName; // Sync with old field if exists
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
