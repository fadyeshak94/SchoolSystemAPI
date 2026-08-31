namespace SchoolSystemAPI.Models;

public class ServiceTask
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public TarbeyaServiceTaskStatus Status { get; set; } = TarbeyaServiceTaskStatus.Pending;

    public int AssignedToServantId { get; set; }
    public AppUser? AssignedToServant { get; set; }

    public int AssignedByAdminId { get; set; }
    public AppUser? AssignedByAdmin { get; set; }

    public int FamilyId { get; set; }
    public TarbeyaFamily? Family { get; set; }
}
