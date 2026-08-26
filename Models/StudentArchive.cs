using System.ComponentModel.DataAnnotations;

namespace SchoolSystemAPI.Models;

public class StudentArchive
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string AcademicYear { get; set; } = string.Empty;

    public int OriginalStudentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string StudentName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ClassName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string StageName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string GovGrade { get; set; } = string.Empty;

    public string PhonesJson { get; set; } = "[]";
}
