using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystemAPI.Models;

public class StudentEnrollment
{
    [Key]
    public int Id { get; set; }

    public int StudentId { get; set; }
    
    [ForeignKey("StudentId")]
    public Student Student { get; set; } = null!;

    public int ClassRoomId { get; set; }
    
    [ForeignKey("ClassRoomId")]
    public ClassRoom ClassRoom { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string AcademicYear { get; set; } = string.Empty;
}
