using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Services;

public class TarbeyaAttendanceService : ITarbeyaAttendanceService
{
    private readonly ApplicationDbContext _context;

    public TarbeyaAttendanceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> RecordAttendanceAsync(int studentId, DateTime date, TarbeyaAttendanceStatus status, int servantId)
    {
        var existing = await _context.TarbeyaAttendances
            .FirstOrDefaultAsync(a => a.StudentId == studentId && a.Date == date.Date);

        if (existing != null)
        {
            existing.Status = status;
        }
        else
        {
            _context.TarbeyaAttendances.Add(new TarbeyaAttendance
            {
                StudentId = studentId,
                Date = date.Date,
                Status = status
            });
        }
        
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> QuickCheckInByBarcodeAsync(string barcode, int servantId)
    {
        var student = await _context.TarbeyaStudents.FirstOrDefaultAsync(s => s.Barcode == barcode);
        if (student == null) return false;

        return await RecordAttendanceAsync(student.Id, DateTime.Today, TarbeyaAttendanceStatus.Present, servantId);
    }
}
