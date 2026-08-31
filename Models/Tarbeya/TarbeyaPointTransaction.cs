namespace SchoolSystemAPI.Models;

public class TarbeyaPointTransaction
{
    public int Id { get; set; }
    
    public int StudentId { get; set; }
    public TarbeyaStudent? Student { get; set; }
    
    public int Amount { get; set; } // Positive for addition, negative for deduction
    public string? Reason { get; set; }
    public DateTime Date { get; set; }
    
    public int? ServantId { get; set; }
    public AppUser? Servant { get; set; }
}
