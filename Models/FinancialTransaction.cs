using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystemAPI.Models;

public class FinancialTransaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Type { get; set; } = "Revenue"; // Revenue or Expense

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public int? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }
}
