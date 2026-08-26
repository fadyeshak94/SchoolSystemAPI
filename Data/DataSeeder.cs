using SchoolSystemAPI.Models;
using SchoolSystemAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace SchoolSystemAPI.Data;

public static class DataSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        // Auto apply pending migrations
        if (context.Database.IsSqlServer())
        {
            await context.Database.MigrateAsync();
        }

        if (!context.Users.Any(u => u.Username == "admin"))
        {
            var salt = authService.GenerateSalt();
            var adminUser = new AppUser
            {
                Username = "admin",
                Role = "Admin",
                Salt = salt,
                PasswordHash = authService.HashPassword("1234", salt)
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }

        // Migrate PhonesJson
        var students = await context.Students.ToListAsync();
        bool changed = false;
        foreach (var s in students)
        {
            if (string.IsNullOrWhiteSpace(s.PhonesJson) || s.PhonesJson == "[]") continue;
            try
            {
                if (s.PhonesJson.Contains("\"number\""))
                {
                    var objs = System.Text.Json.JsonSerializer.Deserialize<List<SchoolSystemAPI.Controllers.PhoneObj>>(s.PhonesJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (objs != null)
                    {
                        foreach (var o in objs) o.whatsapp = true;
                        s.PhonesJson = System.Text.Json.JsonSerializer.Serialize(objs);
                        changed = true;
                    }
                }
                else
                {
                    var arr = System.Text.Json.JsonSerializer.Deserialize<List<string>>(s.PhonesJson);
                    if (arr != null && arr.Any())
                    {
                        var objs = arr.Select(str => new SchoolSystemAPI.Controllers.PhoneObj { number = str, whatsapp = true }).ToList();
                        s.PhonesJson = System.Text.Json.JsonSerializer.Serialize(objs);
                        changed = true;
                    }
                }
            }
            catch { }
        }
        if (changed) await context.SaveChangesAsync();
    }
}
