namespace SchoolSystemAPI.Models;

public class TarbeyaArea
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Navigation property
    public ICollection<TarbeyaStudent> Students { get; set; } = new List<TarbeyaStudent>();
}
