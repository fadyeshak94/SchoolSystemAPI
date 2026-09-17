using System;
using System.Collections.Generic;

namespace SchoolSystemAPI.Models;

public class Mother
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Whatsapp { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string HusbandName { get; set; } = string.Empty;
    public string Occupation { get; set; } = string.Empty;
    public string ConfessionFather { get; set; } = string.Empty;

    // Navigation property for children
    public ICollection<Student> Children { get; set; } = new List<Student>();
    
    // Navigation property for attendances
    public ICollection<MotherAttendance> Attendances { get; set; } = new List<MotherAttendance>();
}
