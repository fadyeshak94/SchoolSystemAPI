namespace SchoolSystemAPI.Models.DTOs;

public class RecordMahraganScoreDto
{
    public int EnrollmentId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public decimal Score { get; set; }
}
