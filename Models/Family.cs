using System.ComponentModel.DataAnnotations;

namespace SchoolSystemAPI.Models;

public class Family
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Address { get; set; } = string.Empty;
    public string FatherPhone { get; set; } = string.Empty;
    public string MotherPhone { get; set; } = string.Empty;

    public ICollection<Student> Siblings { get; set; } = new List<Student>();
}
