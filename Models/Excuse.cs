using System.ComponentModel.DataAnnotations;

namespace SchoolSystemAPI.Models;

public class Excuse
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    [MaxLength(10)]
    public string Term { get; set; } = string.Empty;

    [MaxLength(20)]
    public string AcademicYear { get; set; } = string.Empty;
}
