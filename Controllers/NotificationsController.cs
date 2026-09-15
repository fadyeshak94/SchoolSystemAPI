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
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("register-token")]
    public async Task<IActionResult> RegisterToken([FromBody] RegisterTokenDto dto)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized();

        var existingToken = await _context.FcmTokens
            .FirstOrDefaultAsync(t => t.Token == dto.Token && t.AppUserId == userId);

        if (existingToken == null)
        {
            var newToken = new FcmToken
            {
                Token = dto.Token,
                DeviceType = dto.DeviceType ?? "Unknown",
                Created = DateTime.UtcNow,
                AppUserId = userId
            };
            
            _context.FcmTokens.Add(newToken);
            await _context.SaveChangesAsync();
        }

        return Ok(new { success = true, message = "Token registered successfully" });
    }
}

public class RegisterTokenDto
{
    public string Token { get; set; } = string.Empty;
    public string? DeviceType { get; set; }
}
