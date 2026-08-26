namespace SchoolSystemAPI.Models;

public class AppSetting
{
    public int Id { get; set; }
    public string AcademicYear { get; set; } = "2026-2027";
    public string CurrentTerm { get; set; } = "1";
    public string AdminWhatsapp { get; set; } = string.Empty;
}

public class StageFee
{
    public int Id { get; set; }
    public string StageName { get; set; } = string.Empty; // مثال: "ابتدائي ب"
    public decimal FeeAmount { get; set; }
}
