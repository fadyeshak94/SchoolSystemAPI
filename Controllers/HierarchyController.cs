using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using SchoolSystemAPI.Services;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]

public class HierarchyController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;

    // المواد السبع الرسمية لتسكين الخدام
    public static readonly string[] AllowedSubjects = new[]
    {
        "أجبية",
        "لغة قبطية",
        "ألحان",
        "طقس",
        "كتاب مقدس",
        "عقيدة",
        "تاريخ كنيسة"
    };

    // المراحل الخمس الرسمية
    public static readonly string[] AllowedStages = new[]
    {
        "حضانة",
        "ابتدائي أ",
        "ابتدائي ب",
        "إعدادي",
        "كبار"
    };

    public HierarchyController(IUnitOfWork uow, IAuthService authService, ApplicationDbContext context)
    {
        _uow = uow;
        _authService = authService;
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetHierarchyOverview()
    {
        var users = await _context.Users
            .Include(u => u.ClassRoom)
            .Include(u => u.ServantAssignments)
                .ThenInclude(sa => sa.ClassRoom)
            .AsNoTracking()
            .ToListAsync();

        var classes = await _context.ClassRooms
            .Include(c => c.Students)
            .AsNoTracking()
            .ToListAsync();

        var currentRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User";
        var currentStageAccess = User.FindFirst("StageAccess")?.Value;

        // 1. مجلس الإدارة والناظر
        var board = users.Where(u => u.Role == "Admin" || (u.Title != null && (u.Title.Contains("ناظر") || u.Title.Contains("مجلس") || u.Title.Contains("منتدب"))))
            .Select(u => new
            {
                id = u.Id,
                username = u.Username,
                email = u.Email,
                role = u.Role,
                title = string.IsNullOrEmpty(u.Title) ? "عضو مجلس إدارة" : u.Title,
                isPrincipal = u.Title != null && (u.Title.Contains("الناظر") || u.Title.Contains("العضو المنتدب"))
            })
            .OrderByDescending(u => u.isPrincipal)
            .ToList();

        if (currentRole == "StageSupervisor")
        {
            board.Clear();
        }

        // 2. المراحل الخمس مع أمنائها وفصولها
        var stagesToReturn = AllowedStages.AsEnumerable();
        if (currentRole == "StageSupervisor" && !string.IsNullOrEmpty(currentStageAccess))
        {
            stagesToReturn = stagesToReturn.Where(s => s == currentStageAccess);
        }

        var stagesList = stagesToReturn.Select(stageName =>
        {
            var stageClasses = classes.Where(c => (c.Stage ?? "").Trim() == stageName).ToList();
            var stageClassIds = stageClasses.Select(c => c.Id).ToHashSet();

            var trustees = users.Where(u => u.Role == "StageSupervisor" && (u.StageAccess ?? "").Trim() == stageName)
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    email = u.Email,
                    title = string.IsNullOrEmpty(u.Title) ? $"أمين مرحلة {stageName}" : u.Title
                }).ToList();

            var stageStudentsCount = stageClasses.Sum(c => c.Students.Count);
            
            // الخدام الموزعين على فصول هذه المرحلة
            var stageServantIds = users.Where(u => u.ServantAssignments.Any(sa => stageClassIds.Contains(sa.ClassRoomId)))
                                       .Select(u => u.Id).Distinct().Count();

            return new
            {
                stageName = stageName,
                trustees = trustees,
                studentsCount = stageStudentsCount,
                servantsCount = stageServantIds,
                classesCount = stageClasses.Count,
                classes = stageClasses.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    year = c.Year,
                    studentCount = c.Students.Count
                }).ToList()
            };
        }).ToList();

        // 3. السكرتارية
        var secretariat = users.Where(u => u.Role == "Secretary" || u.Role == "User")
            .Select(u => new
            {
                id = u.Id,
                username = u.Username,
                email = u.Email,
                title = string.IsNullOrEmpty(u.Title) ? "مسؤولة سكرتارية" : u.Title,
                phoneNumber = u.PhoneNumber,
                confessionFather = u.ConfessionFather,
                dateOfBirth = u.DateOfBirth,
                classId = u.ClassRoomId,
                className = u.ClassRoom != null ? u.ClassRoom.Name : null,
                stage = u.ClassRoom != null ? u.ClassRoom.Stage : u.StageAccess
            }).ToList();

        if (currentRole == "StageSupervisor" && !string.IsNullOrEmpty(currentStageAccess))
        {
            secretariat = secretariat.Where(s => s.stage == currentStageAccess || string.IsNullOrEmpty(s.stage)).ToList();
        }

        // 4. الخدام والتكليفات
        var servantsQuery = users.Where(u => u.Role == "Servant" || u.ServantAssignments.Any());
        
        if (currentRole == "StageSupervisor" && !string.IsNullOrEmpty(currentStageAccess))
        {
            servantsQuery = servantsQuery.Where(u => u.ServantAssignments.Any(sa => sa.ClassRoom != null && sa.ClassRoom.Stage == currentStageAccess));
        }

        var servants = servantsQuery
            .Select(u => new
            {
                id = u.Id,
                username = u.Username,
                email = u.Email,
                title = string.IsNullOrEmpty(u.Title) ? "خادم" : u.Title,
                phoneNumber = u.PhoneNumber,
                confessionFather = u.ConfessionFather,
                dateOfBirth = u.DateOfBirth,
                assignments = u.ServantAssignments.Select(sa => new
                {
                    id = sa.Id,
                    classRoomId = sa.ClassRoomId,
                    className = sa.ClassRoom?.Name ?? "غير محدد",
                    stage = sa.ClassRoom?.Stage ?? "",
                    subjectName = sa.SubjectName,
                    academicYear = sa.AcademicYear
                }).Where(a => currentRole != "StageSupervisor" || string.IsNullOrEmpty(currentStageAccess) || a.stage == currentStageAccess).ToList()
            }).ToList();

        var allClasses = classes.AsEnumerable();
        if (currentRole == "StageSupervisor" && !string.IsNullOrEmpty(currentStageAccess))
        {
            allClasses = allClasses.Where(c => c.Stage == currentStageAccess);
        }

        return Ok(new
        {
            success = true,
            board = board,
            stages = stagesList,
            secretariat = secretariat,
            servants = servants,
            allowedSubjects = AllowedSubjects,
            allowedStages = AllowedStages,
            allClasses = allClasses.Select(c => new { id = c.Id, name = c.Name, stage = c.Stage, year = c.Year }).ToList(),
            allUsers = users.Select(u => new { id = u.Id, username = u.Username, role = u.Role, title = u.Title }).ToList()
        });
    }

    [HttpGet("subjects")]
    public IActionResult GetSubjects()
    {
        return Ok(new { success = true, subjects = AllowedSubjects });
    }

    [HttpPost("assign-servant")]
    public async Task<IActionResult> AssignServant([FromBody] AssignServantDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
        if (user == null) return NotFound(new { success = false, message = "المستخدم غير موجود" });

        var classroom = await _context.ClassRooms.FirstOrDefaultAsync(c => c.Id == dto.ClassRoomId);
        if (classroom == null) return NotFound(new { success = false, message = "الفصل غير موجود" });

        if (!AllowedSubjects.Contains(dto.SubjectName))
        {
            return BadRequest(new { success = false, message = "المادة المختارة ليست من المواد السبع المعتمدة" });
        }

        // تحقق من التكرار
        var exists = await _context.ServantAssignments.AnyAsync(sa =>
            sa.UserId == dto.UserId && sa.ClassRoomId == dto.ClassRoomId && sa.SubjectName == dto.SubjectName);

        if (exists)
        {
            return BadRequest(new { success = false, message = "هذا الخادم مسجل بالفعل لنفس الفصل ونفس المادة" });
        }

        // لو المستخدم لم يكن خادماً، نجعل دوره خادماً
        if (user.Role != "Admin" && user.Role != "Secretary" && user.Role != "StageSupervisor")
        {
            user.Role = "Servant";
        }
        if (string.IsNullOrEmpty(user.Title))
        {
            user.Title = "خادم";
        }

        var assignment = new ServantAssignment
        {
            UserId = dto.UserId,
            ClassRoomId = dto.ClassRoomId,
            SubjectName = dto.SubjectName,
            AcademicYear = dto.AcademicYear
        };

        await _context.ServantAssignments.AddAsync(assignment);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "تم تكليف الخادم بالفصل والمادة بنجاح", assignmentId = assignment.Id });
    }

    [HttpPost("add-servant")]
    public async Task<IActionResult> AddServantAndAssign([FromBody] AddServantDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            return BadRequest(new { success = false, message = "اسم المستخدم مطلوب" });
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.Trim().ToLower());
        if (existingUser != null)
        {
            return BadRequest(new { success = false, message = "اسم المستخدم موجود بالفعل، يمكنك اختياره وتسكينه مباشرة" });
        }

        var salt = _authService.GenerateSalt();
        var password = string.IsNullOrWhiteSpace(dto.Password) ? "123456" : dto.Password;
        var hash = _authService.HashPassword(password, salt);

        var newUser = new AppUser
        {
            Username = dto.Username.Trim(),
            Email = dto.Email?.Trim(),
            Role = "Servant",
            Title = string.IsNullOrWhiteSpace(dto.Title) ? "خادم" : dto.Title.Trim(),
            PasswordHash = hash,
            Salt = salt,
            PendingReset = true
        };

        await _context.Users.AddAsync(newUser);
        await _context.SaveChangesAsync();

        // إذا تم تحديد فصل ومادة فوراً
        if (dto.ClassRoomId.HasValue && dto.ClassRoomId.Value > 0 && !string.IsNullOrWhiteSpace(dto.SubjectName))
        {
            if (AllowedSubjects.Contains(dto.SubjectName))
            {
                var assignment = new ServantAssignment
                {
                    UserId = newUser.Id,
                    ClassRoomId = dto.ClassRoomId.Value,
                    SubjectName = dto.SubjectName,
                    AcademicYear = dto.AcademicYear
                };
                await _context.ServantAssignments.AddAsync(assignment);
                await _context.SaveChangesAsync();
            }
        }

        return Ok(new { success = true, message = "تم إنشاء حساب الخادم وتسكينه بنجاح", userId = newUser.Id });
    }

    [HttpDelete("assignment/{id}")]
    public async Task<IActionResult> RemoveAssignment(int id)
    {
        var assignment = await _context.ServantAssignments.FirstOrDefaultAsync(sa => sa.Id == id);
        if (assignment == null) return NotFound(new { success = false, message = "التكليف غير موجود" });

        _context.ServantAssignments.Remove(assignment);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "تم حذف التكليف بنجاح" });
    }

    [HttpPost("assign-trustee")]
    [Authorize(Roles = "Admin")] // للأدمن / مجلس الإدارة فقط
    public async Task<IActionResult> AssignTrustee([FromBody] AssignTrusteeDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
        if (user == null) return NotFound(new { success = false, message = "المستخدم غير موجود" });

        if (!AllowedStages.Contains(dto.Stage))
        {
            return BadRequest(new { success = false, message = "المرحلة غير صحيحة" });
        }

        user.Role = "StageSupervisor";
        user.StageAccess = dto.Stage;
        user.Title = string.IsNullOrWhiteSpace(dto.Title) ? $"أمين مرحلة {dto.Stage}" : dto.Title.Trim();

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = $"تم تعيين {user.Username} كأمين لمرحلة {dto.Stage} بنجاح" });
    }

    [HttpPost("set-title")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetUserTitle([FromBody] SetTitleDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
        if (user == null) return NotFound(new { success = false, message = "المستخدم غير موجود" });

        user.Title = dto.Title;
        if (!string.IsNullOrEmpty(dto.Role))
        {
            user.Role = dto.Role;
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "تم تحديث البيانات الإدارية بنجاح" });
    }
}

public class AssignServantDto
{
    public int UserId { get; set; }
    public int ClassRoomId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
}

public class AddServantDto
{
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Title { get; set; }
    public int? ClassRoomId { get; set; }
    public string? SubjectName { get; set; }
    public string? AcademicYear { get; set; }
}

public class AssignTrusteeDto
{
    public int UserId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string? Title { get; set; }
}

public class SetTitleDto
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Role { get; set; }
}
