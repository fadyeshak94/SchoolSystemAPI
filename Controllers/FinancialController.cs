using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,StageSupervisor,Secretary,HeadSecretary")]
public class FinancialController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FinancialController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetFinancialData()
    {
        // Get all required stage fees
        var stageFees = await _context.StageFees.ToDictionaryAsync(s => s.StageName, s => s.FeeAmount);

        var students = await _context.Students.Include(s => s.ClassRoom).ToListAsync();
        var pendingRegs = await _context.PendingRegistrations.Where(p => p.Status == "Pending" || p.Status == "Waitlisted").ToListAsync();
        
        var classRooms = await _context.ClassRooms.ToDictionaryAsync(c => c.Id, c => c);

        var result = new List<object>();

        // 1. Process Approved Students
        foreach (var s in students)
        {
            var stageName = s.ClassRoom?.Stage ?? "";
            decimal requiredFee = stageFees.ContainsKey(stageName) ? stageFees[stageName] : 0;
            decimal finalRequiredFee = s.HasHalfDiscount ? (requiredFee / 2) : requiredFee;
            decimal remaining = finalRequiredFee - s.AmountPaid;

            result.Add(new
            {
                Type = "Approved",
                Id = s.Id,
                Name = s.Name,
                Stage = stageName,
                ClassName = s.ClassRoom?.Name ?? "",
                Status = "مقبول",
                RequiredFee = requiredFee,
                HasHalfDiscount = s.HasHalfDiscount,
                FinalRequiredFee = finalRequiredFee,
                AmountPaid = s.AmountPaid,
                Remaining = remaining
            });
        }

        // 2. Process Pending Registrations
        foreach (var p in pendingRegs)
        {
            var stageName = classRooms.ContainsKey(p.ClassId) ? classRooms[p.ClassId].Stage : "";
            var className = classRooms.ContainsKey(p.ClassId) ? classRooms[p.ClassId].Name : "";
            
            decimal requiredFee = stageFees.ContainsKey(stageName) ? stageFees[stageName] : 0;
            decimal finalRequiredFee = p.HasHalfDiscount ? (requiredFee / 2) : requiredFee;
            decimal remaining = finalRequiredFee - p.AmountPaid;

            result.Add(new
            {
                Type = "Pending",
                Id = p.Id,
                Name = p.Name,
                Stage = stageName,
                ClassName = className,
                Status = p.Status == "Pending" ? "معلق" : "قائمة انتظار",
                RequiredFee = requiredFee,
                HasHalfDiscount = p.HasHalfDiscount,
                FinalRequiredFee = finalRequiredFee,
                AmountPaid = p.AmountPaid,
                Remaining = remaining
            });
        }

        // Sort by Stage, then Name
        var sortedResult = result.OrderBy(x => ((dynamic)x).Stage).ThenBy(x => ((dynamic)x).Name).ToList();

        return Ok(new { success = true, data = sortedResult });
    }

    [HttpPost("{type}/{id}/discount")]
    [Authorize(Roles = "Admin,HeadSecretary")]
    public async Task<IActionResult> ToggleDiscount(string type, int id)
    {
        if (type == "Approved")
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound(new { success = false, message = "الطالب غير موجود" });

            student.HasHalfDiscount = !student.HasHalfDiscount;
            _context.Students.Update(student);
        }
        else if (type == "Pending")
        {
            var pending = await _context.PendingRegistrations.FindAsync(id);
            if (pending == null) return NotFound(new { success = false, message = "الطلب غير موجود" });

            pending.HasHalfDiscount = !pending.HasHalfDiscount;
            _context.PendingRegistrations.Update(pending);
        }
        else
        {
            return BadRequest(new { success = false, message = "نوع الطلب غير صالح" });
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "تم تعديل الخصم بنجاح" });
    }
}
