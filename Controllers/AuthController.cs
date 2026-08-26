using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Services;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var (success, token, message, pendingReset) = await _authService.LoginAsync(dto.Username, dto.Password);
        
        if (!success)
            return Unauthorized(new { success = false, message });

        return Ok(new { success = true, token, message, requiresPasswordReset = pendingReset });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var (success, message) = await _authService.ChangePasswordAsync(dto.Username, dto.OldPassword, dto.NewPassword);
        if (!success)
            return BadRequest(new { success = false, message });
            
        return Ok(new { success = true, message });
    }
}

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string Username { get; set; } = string.Empty;
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
