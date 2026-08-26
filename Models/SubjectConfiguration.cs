namespace SchoolSystemAPI.Models;

public class SubjectConfiguration
{
    public int Id { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public decimal MaxScoreTerm1 { get; set; }
    public decimal MaxScoreTerm2 { get; set; }
}
