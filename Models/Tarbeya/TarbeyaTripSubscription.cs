namespace SchoolSystemAPI.Models;

public class TarbeyaTripSubscription
{
    public int Id { get; set; }
    
    public int TripId { get; set; }
    public TarbeyaTrip? Trip { get; set; }

    public int StudentId { get; set; }
    public TarbeyaStudent? Student { get; set; }

    public decimal AmountPaid { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.Now;

    public int ServantId { get; set; }
    public AppUser? Servant { get; set; }
}
