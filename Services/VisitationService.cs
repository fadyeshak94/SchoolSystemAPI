using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Services;

public class VisitationService : IVisitationService
{
    private readonly ApplicationDbContext _context;

    public VisitationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TarbeyaStudent>> GetStudentsNeedingVisitationAsync(int? classId, int? familyId)
    {
        var query = _context.TarbeyaStudents
            .Include(s => s.Class)
            .ThenInclude(c => c!.Stage)
            .Include(s => s.AreaNavigation)
            .AsQueryable();

        if (classId.HasValue)
        {
            query = query.Where(s => s.ClassId == classId.Value);
        }
        
        if (familyId.HasValue)
        {
            query = query.Where(s => s.Class!.Stage!.FamilyId == familyId.Value);
        }

        var students = await query.ToListAsync();
        var needsVisitation = new List<TarbeyaStudent>();
        
        var threeWeeksAgo = DateTime.Today.AddDays(-21);

        foreach (var student in students)
        {
            // Get last 3 weeks attendances
            var recentAttendances = await _context.TarbeyaAttendances
                .Where(a => a.StudentId == student.Id && a.Date >= threeWeeksAgo && a.Date <= DateTime.Today)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            if (recentAttendances.Count == 0 || (recentAttendances.Count >= 3 && recentAttendances.Take(3).All(a => a.Status == TarbeyaAttendanceStatus.Absent)))
            {
                needsVisitation.Add(student);
            }
        }

        return needsVisitation;
    }

    public async Task<bool> RecordVisitationAsync(TarbeyaVisitationRecord record)
    {
        _context.TarbeyaVisitationRecords.Add(record);
        return await _context.SaveChangesAsync() > 0;
    }
}
