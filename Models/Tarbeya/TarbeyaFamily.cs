namespace SchoolSystemAPI.Models;

public class TarbeyaFamily
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<TarbeyaStage> Stages { get; set; } = new List<TarbeyaStage>();
}
