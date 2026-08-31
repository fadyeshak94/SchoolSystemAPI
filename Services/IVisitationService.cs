using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Services;

public interface IVisitationService
{
    Task<List<TarbeyaStudent>> GetStudentsNeedingVisitationAsync(int? classId, int? familyId);
    Task<bool> RecordVisitationAsync(TarbeyaVisitationRecord record);
}
