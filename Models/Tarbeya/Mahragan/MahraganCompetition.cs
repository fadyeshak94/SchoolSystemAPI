namespace SchoolSystemAPI.Models;

public class MahraganCompetition
{
    public int Id { get; set; }
    
    public int EventId { get; set; }
    public MahraganEvent? Event { get; set; }

    public string Name { get; set; } = string.Empty; // e.g. "ألحان"
    
    public int TargetStageId { get; set; }
    public TarbeyaStage? TargetStage { get; set; }

    public decimal PassingScore { get; set; }

    public ICollection<MahraganEnrollment> Enrollments { get; set; } = new List<MahraganEnrollment>();
}
