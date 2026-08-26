using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystemAPI.Models;

public class PendingRegistration
{
    [Key]
    public int Id { get; set; }

    public int? StudentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; }

    [MaxLength(20)]
    public string Gender { get; set; }

    public bool IsDeacon { get; set; }

    [MaxLength(50)]
    public string GovGrade { get; set; }

    public string PhonesJson { get; set; }

    public int ClassId { get; set; }

    public decimal AmountPaid { get; set; }

    public bool IsRenewal { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
}
