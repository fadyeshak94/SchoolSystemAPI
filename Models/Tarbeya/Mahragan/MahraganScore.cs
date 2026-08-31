namespace SchoolSystemAPI.Models;

public class MahraganScore
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }
    public MahraganEnrollment? Enrollment { get; set; }

    public string StageName { get; set; } = string.Empty; // e.g., تصفية أولى
    public decimal Score { get; set; }
    public bool IsQualified { get; set; }
}
