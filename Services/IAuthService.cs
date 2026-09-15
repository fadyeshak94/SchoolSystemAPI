using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Services;

public interface IAuthService
{
    string GenerateSalt();
    string HashPassword(string password, string salt);
    string GenerateJwtToken(AppUser user);
    Task<(bool Success, string Token, string RefreshToken, string Message, bool PendingReset)> LoginAsync(string username, string password);
    Task<(bool Success, string Token, string RefreshToken, string Message)> RefreshTokenAsync(string token, string refreshToken);
    Task<bool> RevokeTokenAsync(string token);
    Task<(bool Success, string Message)> ChangePasswordAsync(string username, string oldPassword, string newPassword);
}
