namespace SchoolSystemAPI.Models;

public class TarbeyaClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public int StageId { get; set; }
    public TarbeyaStage? Stage { get; set; }
    
    public ICollection<TarbeyaStudent> Students { get; set; } = new List<TarbeyaStudent>();
}
