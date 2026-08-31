namespace SchoolSystemAPI.Models;

public enum VisitationType { Phone, Home, Church }

public class TarbeyaVisitationRecord
{
    public int Id { get; set; }
    
    public int StudentId { get; set; }
    public TarbeyaStudent? Student { get; set; }
    
    public VisitationType Type { get; set; }
    public DateTime Date { get; set; }
    public string? Result { get; set; }
    
    public int? ServantId { get; set; }
    public AppUser? Servant { get; set; }
}
