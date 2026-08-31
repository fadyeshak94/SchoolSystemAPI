namespace SchoolSystemAPI.Models;

public class TarbeyaTrip
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime TripDate { get; set; }
    public decimal TicketPrice { get; set; }
    
    public int FamilyId { get; set; }
    public TarbeyaFamily? Family { get; set; }

    public ICollection<TarbeyaTripSubscription> Subscriptions { get; set; } = new List<TarbeyaTripSubscription>();
    public ICollection<TarbeyaTripExpense> Expenses { get; set; } = new List<TarbeyaTripExpense>();
}
