using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using SchoolSystemAPI.Models.DTOs;
using System.Security.Claims;
using MediatR;
using SchoolSystemAPI.Features.Notifications;

namespace SchoolSystemAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "TarbeyaGeneralAdmin,TarbeyaFamilyAdmin,TarbeyaServant,Admin")]
    public class TarbeyaServantsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediator _mediator;

        public TarbeyaServantsController(ApplicationDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        private int GetCurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        private string GetCurrentUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        private int? GetUserFamilyId()
        {
            var f = User.FindFirst("TarbeyaFamilyId")?.Value;
            return string.IsNullOrEmpty(f) ? null : int.Parse(f);
        }

        // ======================== ATTENDANCE ========================

        [HttpGet("attendance")]
        public async Task<IActionResult> GetAttendance([FromQuery] DateTime? date)
        {
            var role = GetCurrentUserRole();
            if (role == "TarbeyaServant") return Forbid(); // Servants can't view all attendance

            var familyId = GetUserFamilyId();
            if (role == "TarbeyaFamilyAdmin" && familyId == null) return BadRequest("Family not found");

            var query = _context.ServantAttendances.Include(a => a.Servant).AsQueryable();
            
            if (role == "TarbeyaFamilyAdmin")
                query = query.Where(a => a.FamilyId == familyId);
                
            if (date.HasValue)
                query = query.Where(a => a.Date.Date == date.Value.Date);

            var list = await query.Select(a => new {
                a.Id,
                a.ServantId,
                ServantName = a.Servant.Username,
                a.Date,
                MeetingType = a.MeetingType.ToString(),
                a.Status
            }).ToListAsync();

            return Ok(new { success = true, attendance = list });
        }

        [HttpPost("attendance")]
        public async Task<IActionResult> RegisterAttendance([FromBody] ServantAttendanceDto dto)
        {
            var role = GetCurrentUserRole();
            var familyId = GetUserFamilyId();
            if (role != "TarbeyaFamilyAdmin" || familyId == null) return Forbid();

            // Use MediatR for CQRS and Triggering Events
            var result = await _mediator.Send(new RecordServantAttendanceCommand
            {
                ServantId = dto.ServantId,
                FamilyId = familyId.Value,
                Date = dto.Date,
                MeetingType = dto.MeetingType ?? "PrepMeeting",
                Status = dto.Status ?? "Present"
            });

            return Ok(new { success = true });
        }

        // ======================== TASKS ========================

        [HttpGet("tasks")]
        public async Task<IActionResult> GetTasks()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            var familyId = GetUserFamilyId();

            var query = _context.ServiceTasks
                .Include(t => t.AssignedToServant)
                .Include(t => t.AssignedByAdmin)
                .AsQueryable();

            if (role == "TarbeyaServant")
            {
                query = query.Where(t => t.AssignedToServantId == userId);
            }
            else if (role == "TarbeyaFamilyAdmin")
            {
                query = query.Where(t => t.FamilyId == familyId);
            }

            var tasks = await query.Select(t => new {
                t.Id,
                t.Title,
                t.Description,
                t.DueDate,
                t.Status,
                StatusName = t.Status.ToString(),
                AssignedToName = t.AssignedToServant.Username,
                AssignedByName = t.AssignedByAdmin.Username
            }).ToListAsync();

            return Ok(new { success = true, tasks });
        }

        [HttpPost("tasks")]
        public async Task<IActionResult> CreateTask([FromBody] CreateServiceTaskDto dto)
        {
            var role = GetCurrentUserRole();
            if (role == "TarbeyaServant") return Forbid(); // Servants cannot assign tasks
            
            var familyId = GetUserFamilyId();
            if (role == "TarbeyaFamilyAdmin" && familyId == null) return BadRequest("Family not found");

            var task = new ServiceTask
            {
                Title = dto.Title,
                Description = dto.Description,
                DueDate = dto.DueDate,
                AssignedToServantId = dto.AssignedToServantId,
                AssignedByAdminId = GetCurrentUserId(),
                FamilyId = familyId ?? 0, // Fallback for GeneralAdmin, though they should provide it if needed
                Status = TarbeyaServiceTaskStatus.Pending
            };

            _context.ServiceTasks.Add(task);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Task created" });
        }

        [HttpPut("tasks/{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] int status)
        {
            var task = await _context.ServiceTasks.FindAsync(id);
            if (task == null) return NotFound("Task not found");

            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            
            // Only the assigned servant or an admin can update
            if (role == "TarbeyaServant" && task.AssignedToServantId != userId) return Forbid();
            if (role == "TarbeyaFamilyAdmin" && task.FamilyId != GetUserFamilyId()) return Forbid();

            task.Status = (TarbeyaServiceTaskStatus)status;
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }

    public class ServantAttendanceDto
    {
        public int ServantId { get; set; }
        public DateTime Date { get; set; }
        public string? MeetingType { get; set; }
        public string? Status { get; set; }
    }
}
