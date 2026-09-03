using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using SchoolSystemAPI.Services;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthService _authService;

    public UsersController(IUnitOfWork uow, IAuthService authService)
    {
        _uow = uow;
        _authService = authService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")] // للأدمن فقط
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _uow.Users.FindAsync(u => true);
        var result = users.Select(u => new
        {
            id = u.Id,
            username = u.Username,
            email = u.Email,
            role = u.Role,
            title = u.Title,
            classId = u.ClassRoomId,
            stageAccess = u.StageAccess,
            phoneNumber = u.PhoneNumber,
            confessionFather = u.ConfessionFather,
            dateOfBirth = u.DateOfBirth,
            hasPassword = !string.IsNullOrEmpty(u.PasswordHash)
        }).ToList();

        var classes = await _uow.ClassRooms.FindAsync(c => true);
        var stages = classes.Select(c => c.Stage).Distinct().ToList();

        return Ok(new 
        { 
            success = true, 
            users = result,
            classes = classes.Select(c => new { id = c.Id, name = c.Name, stage = c.Stage, year = c.Year }).ToList(),
            stages = stages
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddUser([FromBody] AddUserDto dto)
    {
        var existingUser = await _uow.Users.FindAsync(u => u.Username.ToLower() == dto.Username.ToLower());
        if (existingUser.Any())
            return BadRequest(new { success = false, message = "اسم المستخدم موجود بالفعل" });

        var salt = _authService.GenerateSalt();
        var hash = string.IsNullOrEmpty(dto.Password) ? "" : _authService.HashPassword(dto.Password, salt);

        var newUser = new AppUser
        {
            Username = dto.Username,
            Email = dto.Email,
            Role = dto.Role,
            Title = dto.Title,
            ClassRoomId = dto.ClassId,
            StageAccess = dto.StageAccess,
            PhoneNumber = dto.PhoneNumber,
            ConfessionFather = dto.ConfessionFather,
            DateOfBirth = dto.DateOfBirth,
            PasswordHash = hash,
            Salt = salt,
            PendingReset = (dto.Role == "Servant")
        };

        await _uow.Users.AddAsync(newUser);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم إضافة المستخدم بنجاح" });
    }

    [HttpPut("{username}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(string username, [FromBody] UpdateUserDto dto)
    {
        var users = await _uow.Users.FindAsync(u => u.Username.ToLower() == username.ToLower());
        var user = users.FirstOrDefault();
        if (user == null) return NotFound(new { success = false, message = "المستخدم غير موجود" });

        user.ClassRoomId = dto.ClassId;
        user.StageAccess = dto.StageAccess;
        user.Title = dto.Title;
        user.PhoneNumber = dto.PhoneNumber;
        user.ConfessionFather = dto.ConfessionFather;
        user.DateOfBirth = dto.DateOfBirth;
        if (!string.IsNullOrEmpty(dto.Role))
        {
            user.Role = dto.Role;
        }

        _uow.Users.Update(user);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم تحديث بيانات المستخدم بنجاح" });
    }

    [HttpPut("{username}/personal")]
    [Authorize(Roles = "Admin,StageSupervisor")]
    public async Task<IActionResult> UpdatePersonalData(string username, [FromBody] UpdatePersonalDataDto dto)
    {
        var users = await _uow.Users.FindAsync(u => u.Username.ToLower() == username.ToLower());
        var user = users.FirstOrDefault();
        if (user == null) return NotFound(new { success = false, message = "المستخدم غير موجود" });

        // التحقق من الصلاحيات لأمين المرحلة (يمكنه فقط تعديل خدام مرحلته)
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var currentStageAccess = User.FindFirst("StageAccess")?.Value;
        if (role == "StageSupervisor")
        {
            bool isServantInStage = user.ServantAssignments.Any(sa => sa.ClassRoom != null && sa.ClassRoom.Stage == currentStageAccess);
            bool isSecretariatInStage = user.StageAccess == currentStageAccess || (user.ClassRoom != null && user.ClassRoom.Stage == currentStageAccess);
            if (!isServantInStage && !isSecretariatInStage && user.Role != "StageSupervisor")
            {
                return Forbid();
            }
        }

        user.PhoneNumber = dto.PhoneNumber;
        user.ConfessionFather = dto.ConfessionFather;
        user.DateOfBirth = dto.DateOfBirth;

        _uow.Users.Update(user);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم تحديث البيانات الشخصية بنجاح" });
    }

    [HttpPut("{username}/password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetPassword(string username, [FromBody] SetPasswordDto dto)
    {
        var users = await _uow.Users.FindAsync(u => u.Username.ToLower() == username.ToLower());
        var user = users.FirstOrDefault();
        if (user == null) return NotFound(new { success = false, message = "المستخدم غير موجود" });

        var salt = _authService.GenerateSalt();
        user.PasswordHash = _authService.HashPassword(dto.NewPassword, salt);
        user.Salt = salt;

        _uow.Users.Update(user);
        await _uow.CompleteAsync();

        return Ok(new { success = true, message = "تم تحديث كلمة المرور بنجاح" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var users = await _uow.Users.FindAsync(u => u.Username.ToLower() == dto.Username.ToLower());
        var user = users.FirstOrDefault();
        
        if (user == null) return NotFound(new { success = false, message = "اسم المستخدم غير موجود" });

        var settings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        
        return Ok(new { success = true, username = user.Username, adminWhatsapp = settings?.AdminWhatsapp });
    }

    [HttpGet("me/classes")]
    [Authorize]
    public async Task<IActionResult> GetMyClasses()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Secretary";
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var classIdStr = User.FindFirst("ClassRoomId")?.Value;
        var stageAccess = User.FindFirst("StageAccess")?.Value;

        var allClassRooms = await _uow.ClassRooms.FindAsync(c => true);

        // الأدمن لديه الرؤية الشاملة لكامل النظام
        if (role == "Admin" || role == "User")
        {
            if (!string.IsNullOrEmpty(classIdStr) && int.TryParse(classIdStr, out int directClassId))
            {
                var filtered = allClassRooms.Where(c => c.Id == directClassId).ToList();
                if (filtered.Any())
                {
                    return Ok(new { success = true, classes = filtered.Select(c => new { id = c.Id, name = c.Name, stage = c.Stage, year = c.Year }).ToList() });
                }
            }

            return Ok(new { success = true, classes = allClassRooms.Select(c => new { id = c.Id, name = c.Name, stage = c.Stage, year = c.Year }).ToList() });
        }

        // السكرتارية وأمين المرحلة: فصول مرحلته أو فصله
        if (role == "StageSupervisor" || role == "Secretary")
        {
            var scopedAllowedClasses = allClassRooms.AsEnumerable();
            bool restricted = false;

            if (!string.IsNullOrEmpty(classIdStr) && int.TryParse(classIdStr, out int scopedClassId))
            {
                scopedAllowedClasses = scopedAllowedClasses.Where(c => c.Id == scopedClassId);
                restricted = true;
            }
            else if (!string.IsNullOrEmpty(stageAccess))
            {
                scopedAllowedClasses = scopedAllowedClasses.Where(c => (c.Stage ?? "").Trim().Equals(stageAccess.Trim(), StringComparison.OrdinalIgnoreCase));
                restricted = true;
            }

            if (restricted)
            {
                return Ok(new { success = true, classes = scopedAllowedClasses.Select(c => new { id = c.Id, name = c.Name, stage = c.Stage, year = c.Year }).ToList() });
            }
            else
            {
                // إذا لم يتم تعيين فصل أو مرحلة له، لا يرى شيئاً
                return Ok(new { success = true, classes = new List<object>() });
            }
        }

        // الخادم: الفصول المسندة إليه في جدول التكليفات
        if (int.TryParse(userIdStr, out int userId))
        {
            var assignments = await _uow.ServantAssignments.FindAsync(sa => sa.UserId == userId);
            if (assignments.Any())
            {
                var assignedClassIds = assignments.Select(sa => sa.ClassRoomId).Distinct().ToHashSet();
                var servantClasses = allClassRooms.Where(c => assignedClassIds.Contains(c.Id)).ToList();
                var assignedSubjects = assignments.Select(sa => new { classId = sa.ClassRoomId, subject = sa.SubjectName }).ToList();

                return Ok(new 
                { 
                    success = true, 
                    classes = servantClasses.Select(c => new { id = c.Id, name = c.Name, stage = c.Stage, year = c.Year }).ToList(),
                    assignments = assignedSubjects
                });
            }
        }

        // حالة احتياطية
        var allowedClasses = allClassRooms.AsEnumerable();
        if (!string.IsNullOrEmpty(classIdStr) && int.TryParse(classIdStr, out int classId))
        {
            allowedClasses = allowedClasses.Where(c => c.Id == classId);
        }
        else if (!string.IsNullOrEmpty(stageAccess))
        {
            allowedClasses = allowedClasses.Where(c => c.Stage == stageAccess);
        }

        return Ok(new { success = true, classes = allowedClasses.Select(c => new { id = c.Id, name = c.Name, stage = c.Stage, year = c.Year }).ToList() });
    }
}

public class AddUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Secretary";
    public string? Title { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ConfessionFather { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? ClassId { get; set; }
    public string? StageAccess { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    public int? ClassId { get; set; }
    public string? StageAccess { get; set; }
    public string? Role { get; set; }
    public string? Title { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ConfessionFather { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

public class UpdatePersonalDataDto
{
    public string? PhoneNumber { get; set; }
    public string? ConfessionFather { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

public class SetPasswordDto { public string NewPassword { get; set; } = string.Empty; }
public class ForgotPasswordDto { public string Username { get; set; } = string.Empty; }
