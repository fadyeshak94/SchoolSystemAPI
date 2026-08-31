namespace SchoolSystemAPI.Models;

public class MahraganEnrollment
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public TarbeyaStudent? Student { get; set; }

    public int CompetitionId { get; set; }
    public MahraganCompetition? Competition { get; set; }

    public string BarcodeString { get; set; } = string.Empty;

    public ICollection<MahraganScore> Scores { get; set; } = new List<MahraganScore>();
}
