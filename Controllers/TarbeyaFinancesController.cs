using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using System.Security.Claims;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TarbeyaFinancesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TarbeyaFinancesController(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<AppUser?> GetCurrentUserAsync()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username)) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    [HttpGet]
    public async Task<IActionResult> GetFinances()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        if (user.Role != "Admin" && user.Role != "TarbeyaGeneralAdmin" && user.Role != "TarbeyaFamilyAdmin")
            return Forbid();

        var query = _context.TarbeyaFamilyTransactions.Include(t => t.Family).Include(t => t.AddedByUser).AsQueryable();

        if (user.Role == "TarbeyaFamilyAdmin")
        {
            if (user.TarbeyaFamilyId == null) return Ok(new { success = true, transactions = new List<object>(), totalIncome = 0, totalExpense = 0, balance = 0 });
            query = query.Where(t => t.FamilyId == user.TarbeyaFamilyId);
        }

        var transactionsList = await query.OrderByDescending(t => t.Date).ToListAsync();

        var totalIncome = transactionsList.Where(t => t.Type == "Income").Sum(t => t.Amount);
        var totalExpense = transactionsList.Where(t => t.Type == "Expense").Sum(t => t.Amount);
        var balance = totalIncome - totalExpense;

        var result = transactionsList.Select(t => new {
            t.Id,
            t.Type,
            t.Category,
            t.Amount,
            t.Description,
            t.Date,
            FamilyName = t.Family?.Name,
            AddedBy = t.AddedByUser?.Username
        });

        return Ok(new { success = true, transactions = result, totalIncome, totalExpense, balance });
    }

    [HttpPost]
    public async Task<IActionResult> AddTransaction([FromBody] AddTransactionDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        if (user.Role != "Admin" && user.Role != "TarbeyaGeneralAdmin" && user.Role != "TarbeyaFamilyAdmin")
            return Forbid();

        int familyIdToUse = 0;
        if (user.Role == "TarbeyaFamilyAdmin")
        {
            if (user.TarbeyaFamilyId == null) return BadRequest(new { success = false, message = "لا تنتمي لأي أسرة" });
            familyIdToUse = user.TarbeyaFamilyId.Value;
        }
        else
        {
            if (dto.FamilyId == null || dto.FamilyId == 0) return BadRequest(new { success = false, message = "FamilyId required for General Admin" });
            familyIdToUse = dto.FamilyId.Value;
        }

        if (dto.Amount <= 0) return BadRequest(new { success = false, message = "Invalid amount" });
        if (dto.Type != "Income" && dto.Type != "Expense") return BadRequest(new { success = false, message = "Invalid type" });

        var transaction = new TarbeyaFamilyTransaction
        {
            FamilyId = familyIdToUse,
            Type = dto.Type,
            Category = dto.Category ?? string.Empty,
            Amount = dto.Amount,
            Description = dto.Description ?? string.Empty,
            Date = DateTime.Now,
            AddedByUserId = user.Id
        };

        _context.TarbeyaFamilyTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var trans = await _context.TarbeyaFamilyTransactions.FindAsync(id);
        if (trans == null) return NotFound(new { success = false, message = "غير موجود" });

        if (user.Role == "TarbeyaFamilyAdmin" && trans.FamilyId != user.TarbeyaFamilyId)
            return Forbid();
        if (user.Role != "Admin" && user.Role != "TarbeyaGeneralAdmin" && user.Role != "TarbeyaFamilyAdmin")
            return Forbid();

        _context.TarbeyaFamilyTransactions.Remove(trans);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }
}

public class AddTransactionDto
{
    public int? FamilyId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
