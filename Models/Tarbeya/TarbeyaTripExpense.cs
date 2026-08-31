namespace SchoolSystemAPI.Models;

public class TarbeyaTripExpense
{
    public int Id { get; set; }
    
    public int TripId { get; set; }
    public TarbeyaTrip? Trip { get; set; }

    public string ItemDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Now;

    public int AddedByFamilyAdminId { get; set; }
    public AppUser? AddedByFamilyAdmin { get; set; }
}
