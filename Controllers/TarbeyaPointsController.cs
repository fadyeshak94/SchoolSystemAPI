using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Models;
using System.Security.Claims;
using SchoolSystemAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TarbeyaPointsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TarbeyaPointsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<AppUser?> GetCurrentUserAsync()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username)) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchStudent([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Query is empty");

        var students = await _context.TarbeyaStudents
            .Include(s => s.Class)
            .Where(s => s.Barcode == query || s.Name.Contains(query))
            .Take(10)
            .Select(s => new {
                s.Id,
                s.Name,
                s.Barcode,
                s.TotalPoints,
                ClassName = s.Class != null ? s.Class.Name : ""
            })
            .ToListAsync();

        return Ok(new { success = true, students });
    }

    [HttpGet("{studentId}/transactions")]
    public async Task<IActionResult> GetTransactions(int studentId)
    {
        var transactions = await _context.TarbeyaPointTransactions
            .Include(t => t.Servant)
            .Where(t => t.StudentId == studentId)
            .OrderByDescending(t => t.Date)
            .Take(50)
            .Select(t => new {
                t.Id,
                t.Amount,
                t.Reason,
                t.Date,
                ServantName = t.Servant != null ? t.Servant.Username : "Unknown"
            })
            .ToListAsync();

        return Ok(new { success = true, transactions });
    }

    [HttpPost]
    public async Task<IActionResult> AddTransaction([FromBody] AddPointTransactionDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var student = await _context.TarbeyaStudents.FirstOrDefaultAsync(s => s.Id == dto.StudentId);
        if (student == null) return NotFound("Student not found");

        var transaction = new TarbeyaPointTransaction
        {
            StudentId = dto.StudentId,
            Amount = dto.Amount,
            Reason = dto.Reason,
            Date = DateTime.Now,
            ServantId = user.Id
        };

        student.TotalPoints += dto.Amount; // Update Total points
        
        _context.TarbeyaPointTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, newTotal = student.TotalPoints, message = "تم تسجيل النقاط بنجاح." });
    }
}

public class AddPointTransactionDto
{
    public int StudentId { get; set; }
    public int Amount { get; set; }
    public string? Reason { get; set; }
}
