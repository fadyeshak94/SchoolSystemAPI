using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs()
    {
        // جلب آخر 500 عملية لضمان سرعة التحميل
        var logs = await _context.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(500)
            .Select(l => new {
                l.Id,
                l.Username,
                l.Action,
                l.EntityName,
                l.Timestamp,
                l.Changes
            })
            .ToListAsync();

        return Ok(new { success = true, logs });
    }
}
