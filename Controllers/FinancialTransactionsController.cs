using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using System.Security.Claims;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,HeadSecretary")]
public class FinancialTransactionsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public FinancialTransactionsController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpPost]
    public async Task<IActionResult> AddTransaction([FromBody] AddTransactionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Description) || dto.Amount <= 0)
        {
            return BadRequest(new { success = false, message = "بيانات الحركة غير صحيحة" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = null;
        if (int.TryParse(userIdClaim, out int uid))
        {
            userId = uid;
        }

        var transaction = new FinancialTransaction
        {
            Type = dto.Type == "Expense" ? "Expense" : "Revenue",
            Description = dto.Description,
            Amount = dto.Amount,
            TransactionDate = DateTime.UtcNow, // could allow custom date in future if needed
            AppUserId = userId
        };

        await _uow.FinancialTransactions.AddAsync(transaction);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم تسجيل الحركة بنجاح", data = transaction });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var transaction = await _uow.FinancialTransactions.GetByIdAsync(id);
        if (transaction == null)
        {
            return NotFound(new { success = false, message = "الحركة غير موجودة" });
        }

        _uow.FinancialTransactions.Remove(transaction);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم حذف الحركة بنجاح" });
    }
}

public class AddTransactionDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
