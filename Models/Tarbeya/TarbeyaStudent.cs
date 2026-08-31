namespace SchoolSystemAPI.Models;

public class TarbeyaStudent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    
    public int? AreaId { get; set; }
    public TarbeyaArea? AreaNavigation { get; set; }
    
    public string? ConfessionFather { get; set; }
    
    public string? GeneralNotes { get; set; }

    // Spiritual Tracking
    public DateTime? LastConfessionDate { get; set; }
    public string? FatherConfessorName { get; set; }

    public string? PrivateNotes { get; set; }
    
    public int? ClassId { get; set; }
    public TarbeyaClass? Class { get; set; }
    
    // Enterprise Features
    public string? ParentPhone { get; set; }
    public string? MedicalNotes { get; set; }
    public string? Barcode { get; set; }
    public int TotalPoints { get; set; } = 0;

    public ICollection<TarbeyaAttendance> Attendances { get; set; } = new List<TarbeyaAttendance>();
    public ICollection<TarbeyaVisitationRecord> Visitations { get; set; } = new List<TarbeyaVisitationRecord>();
    public ICollection<TarbeyaPointTransaction> PointTransactions { get; set; } = new List<TarbeyaPointTransaction>();
}
