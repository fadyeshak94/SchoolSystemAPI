namespace SchoolSystemAPI.Models;

public class ClassRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; 
    public string Stage { get; set; } = string.Empty; 
    public string Year { get; set; } = string.Empty; 
    
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<AppUser> SupervisedByUsers { get; set; } = new List<AppUser>();
}
