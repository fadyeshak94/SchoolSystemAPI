using System;

namespace SchoolSystemAPI.Models;

public class ServantAttendance
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty; // Present, Absent, Excused
    public string AcademicYear { get; set; } = string.Empty;

    public int ServantId { get; set; }
    public AppUser Servant { get; set; } = null!;

    public bool IsExcused { get; set; } = false;
}
