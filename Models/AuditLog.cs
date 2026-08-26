using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystemAPI.Models;

public class AuditLog
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Insert, Update, Delete

    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Optional: store changes as JSON
    public string Changes { get; set; } = string.Empty;
}
