using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public string GenerateSalt()
    {
        return Guid.NewGuid().ToString(); // مطابق للـ Utilities.getUuid() في جوجل سكريبت
    }

    public string HashPassword(string password, string salt)
    {
        // نفس لوجيك الـ Apps Script القديم بالظبط لضمان التوافق
        using var sha256 = SHA256.Create();
        var combined = $"{password}::{salt}";
        var bytes = Encoding.UTF8.GetBytes(combined);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLower();
    }

    public async Task<(bool Success, string Token, string Message, bool PendingReset)> LoginAsync(string username, string password)
    {
        var user = await _context.Users
            .Include(u => u.ClassRoom)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
            return (false, string.Empty, "اسم المستخدم غير صحيح.", false);

        if (string.IsNullOrEmpty(user.PasswordHash))
            return (false, string.Empty, "الحساب ده لسه مفيهوش كلمة مرور متسجلة.", false);

        var inputHash = HashPassword(password, user.Salt);

        if (inputHash != user.PasswordHash)
            return (false, string.Empty, "كلمة المرور غير صحيحة.", false);

        var token = GenerateJwtToken(user);
        return (true, token, "تم تسجيل الدخول بنجاح.", user.PendingReset);
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(string username, string oldPassword, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return (false, "المستخدم غير موجود");

        var oldHash = HashPassword(oldPassword, user.Salt);
        if (oldHash != user.PasswordHash) return (false, "كلمة المرور الحالية غير صحيحة");

        user.Salt = GenerateSalt();
        user.PasswordHash = HashPassword(newPassword, user.Salt);
        user.PendingReset = false;

        await _context.SaveChangesAsync();
        return (true, "تم تغيير كلمة المرور بنجاح");
    }

    public string GenerateJwtToken(AppUser user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var effectiveRole = user.Role == "User" ? "Secretary" : user.Role;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, effectiveRole),
            new Claim("Role", effectiveRole),
            new Claim("ClassRoomId", user.ClassRoomId?.ToString() ?? string.Empty),
            new Claim("StageAccess", user.StageAccess ?? string.Empty),
            new Claim("Title", user.Title ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(16), // صلاحية 16 ساعة زي القديم
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
