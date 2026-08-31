namespace SchoolSystemAPI.Models;

public class TarbeyaFamilyTransaction
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public TarbeyaFamily? Family { get; set; }
    
    public string Type { get; set; } = "Income"; // Income or Expense
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    
    public int AddedByUserId { get; set; }
    public AppUser? AddedByUser { get; set; }
}
