namespace SchoolSystemAPI.Models;

public class TarbeyaStage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public int FamilyId { get; set; }
    public TarbeyaFamily? Family { get; set; }
    
    public ICollection<TarbeyaClass> Classes { get; set; } = new List<TarbeyaClass>();
}
