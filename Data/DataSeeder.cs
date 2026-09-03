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
            try
            {
                await context.Database.MigrateAsync();
            }
            catch { }

            // Ensure Title column exists
            try
            {
                await context.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('Users', 'Title') IS NULL ALTER TABLE Users ADD Title NVARCHAR(150) NULL;");
            }
            catch { }

            // Ensure ServantAssignments table exists
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF OBJECT_ID('ServantAssignments', 'U') IS NULL
                    BEGIN
                        CREATE TABLE ServantAssignments (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            UserId INT NOT NULL,
                            ClassRoomId INT NOT NULL,
                            SubjectName NVARCHAR(100) NOT NULL,
                            AcademicYear NVARCHAR(20) NULL,
                            CONSTRAINT FK_ServantAssignments_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                            CONSTRAINT FK_ServantAssignments_ClassRooms_ClassRoomId FOREIGN KEY (ClassRoomId) REFERENCES ClassRooms(Id) ON DELETE CASCADE
                        );
                        CREATE UNIQUE INDEX IX_ServantAssignments_UserId_ClassRoomId_SubjectName ON ServantAssignments(UserId, ClassRoomId, SubjectName);
                    END");
            }
            catch { }
        }

        var adminUsers = await context.Users.Where(u => u.Username.ToLower() == "admin").ToListAsync();
        if (!adminUsers.Any())
        {
            var salt = authService.GenerateSalt();
            var adminUser = new AppUser
            {
                Username = "admin",
                Role = "Admin",
                Title = "الناظر (العضو المنتدب المفوض بالإدارة)",
                Salt = salt,
                PasswordHash = authService.HashPassword("1234", salt)
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }
        else
        {
            foreach (var a in adminUsers)
            {
                a.Role = "Admin";
                if (string.IsNullOrEmpty(a.Title) || a.Title == "مسؤولة سكرتارية") 
                {
                    a.Title = "الناظر (العضو المنتدب المفوض بالإدارة)";
                }
            }
            await context.SaveChangesAsync();
        }

        // Migrate all legacy 'User' accounts to 'Secretary' smoothly without changing usernames or passwords!
        var legacyUsers = await context.Users.Where(u => u.Role == "User").ToListAsync();
        if (legacyUsers.Any())
        {
            foreach (var u in legacyUsers)
            {
                u.Role = "Secretary";
                if (string.IsNullOrEmpty(u.Title))
                {
                    u.Title = "مسؤولة سكرتارية";
                }
            }
            await context.SaveChangesAsync();
        }

        // Set default title for admin if missing
        var adminsWithoutTitle = await context.Users.Where(u => u.Role == "Admin" && string.IsNullOrEmpty(u.Title)).ToListAsync();
        if (adminsWithoutTitle.Any())
        {
            foreach (var a in adminsWithoutTitle)
            {
                a.Title = "الناظر (العضو المنتدب المفوض بالإدارة)";
            }
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
