namespace SchoolSystemAPI.Models;

public class MahraganEvent
{
    public int Id { get; set; }
    public int Year { get; set; }
    public string ThemeName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<MahraganCompetition> Competitions { get; set; } = new List<MahraganCompetition>();
}
