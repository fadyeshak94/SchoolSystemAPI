using System;

namespace SchoolSystemAPI.Models;

public class MotherAttendance
{
    public int Id { get; set; }
    
    public int MotherId { get; set; }
    public Mother Mother { get; set; } = null!;
    
    public DateTime Date { get; set; }
    
    // Status can be: "حاضر", "غائب", "بعذر"
    public string Status { get; set; } = string.Empty;
}
