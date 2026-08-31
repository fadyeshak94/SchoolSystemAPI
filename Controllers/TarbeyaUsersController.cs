using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using SchoolSystemAPI.Services;
using System.Security.Claims;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TarbeyaUsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;

    public TarbeyaUsersController(ApplicationDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    private async Task<AppUser?> GetCurrentUserAsync()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username)) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var query = _context.Users
            .Where(u => u.Role.StartsWith("Tarbeya"))
            .Include(u => u.TarbeyaFamily)
            .Include(u => u.TarbeyaClass)
            .AsQueryable();

        if (user.Role == "TarbeyaFamilyAdmin")
        {
            query = query.Where(u => u.TarbeyaFamilyId == user.TarbeyaFamilyId || (u.TarbeyaClass != null && u.TarbeyaClass.Stage != null && u.TarbeyaClass.Stage.FamilyId == user.TarbeyaFamilyId));
        }
        else if (user.Role != "Admin" && user.Role != "TarbeyaGeneralAdmin")
        {
            return Forbid();
        }

        var users = await query.Select(u => new
        {
            u.Id,
            u.Username,
            u.Role,
            u.TarbeyaFamilyId,
            FamilyName = u.TarbeyaFamily != null ? u.TarbeyaFamily.Name : null,
            u.TarbeyaClassId,
            ClassName = u.TarbeyaClass != null ? u.TarbeyaClass.Name : null
        }).ToListAsync();

        return Ok(new { success = true, users });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,TarbeyaGeneralAdmin,TarbeyaFamilyAdmin")]
    public async Task<IActionResult> AddUser([FromBody] AddTarbeyaUserDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        if (user.Role == "TarbeyaFamilyAdmin")
        {
            if (dto.Role != "TarbeyaServant") return Forbid("You can only create servants.");
            // ensure class belongs to family
            var targetClass = await _context.TarbeyaClasses.Include(c => c.Stage).FirstOrDefaultAsync(c => c.Id == dto.TarbeyaClassId);
            if (targetClass?.Stage?.FamilyId != user.TarbeyaFamilyId) return Forbid();
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.ToLower());
        if (existingUser != null)
            return BadRequest(new { success = false, message = "اسم المستخدم موجود بالفعل" });

        var salt = _authService.GenerateSalt();
        var hash = string.IsNullOrEmpty(dto.Password) ? "" : _authService.HashPassword(dto.Password, salt);

        var newUser = new AppUser
        {
            Username = dto.Username,
            Role = dto.Role,
            PasswordHash = hash,
            Salt = salt,
            TarbeyaFamilyId = dto.TarbeyaFamilyId,
            TarbeyaClassId = dto.TarbeyaClassId
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "تم إضافة المستخدم بنجاح" });
    }

    [HttpPut("{id}/password")]
    [Authorize(Roles = "Admin,TarbeyaGeneralAdmin")]
    public async Task<IActionResult> SetPassword(int id, [FromBody] SetPasswordDto dto)
    {
        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (targetUser == null) return NotFound(new { success = false, message = "المستخدم غير موجود" });

        var salt = _authService.GenerateSalt();
        targetUser.PasswordHash = _authService.HashPassword(dto.NewPassword, salt);
        targetUser.Salt = salt;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "تم تحديث كلمة المرور بنجاح" });
    }
}

public class AddTarbeyaUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int? TarbeyaFamilyId { get; set; }
    public int? TarbeyaClassId { get; set; }
}
