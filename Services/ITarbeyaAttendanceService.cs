using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Services;

public interface ITarbeyaAttendanceService
{
    Task<bool> RecordAttendanceAsync(int studentId, DateTime date, TarbeyaAttendanceStatus status, int servantId);
    Task<bool> QuickCheckInByBarcodeAsync(string barcode, int servantId);
}
