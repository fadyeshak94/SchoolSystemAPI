namespace SchoolSystemAPI.Models;

public class ServantAssignment
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public int ClassRoomId { get; set; }
    public ClassRoom ClassRoom { get; set; } = null!;

    public string SubjectName { get; set; } = string.Empty; // إحدى المواد السبع: أجبية، لغة قبطية، ألحان، طقس، كتاب مقدس، عقيدة، تاريخ كنيسة
    public string? AcademicYear { get; set; }
}
