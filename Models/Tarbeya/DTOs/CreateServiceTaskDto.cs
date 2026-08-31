namespace SchoolSystemAPI.Models.DTOs;

public class CreateServiceTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int AssignedToServantId { get; set; }
}
