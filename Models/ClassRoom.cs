namespace SchoolSystemAPI.Models;

public class ClassRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; 
    public string Stage { get; set; } = string.Empty; 
    public string Year { get; set; } = string.Empty; 
    
    public ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();
    public ICollection<AppUser> SupervisedByUsers { get; set; } = new List<AppUser>();
    public ICollection<ServantAssignment> ServantAssignments { get; set; } = new List<ServantAssignment>();
}

