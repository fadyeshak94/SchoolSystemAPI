using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;
using System.Security.Claims;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TarbeyaTripsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TarbeyaTripsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<AppUser?> GetCurrentUserAsync()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username)) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var query = _context.TarbeyaTrips.Include(t => t.Family).AsQueryable();

        if (user.Role == "TarbeyaFamilyAdmin")
        {
            if (user.TarbeyaFamilyId == null) return Ok(new { success = true, trips = new List<object>() });
            query = query.Where(t => t.FamilyId == user.TarbeyaFamilyId);
        }
        else if (user.Role == "TarbeyaServant")
        {
            // Servant can only see trips for their class's family
            if (user.TarbeyaClassId == null) return Ok(new { success = true, trips = new List<object>() });
            var classObj = await _context.TarbeyaClasses.Include(c => c.Stage).FirstOrDefaultAsync(c => c.Id == user.TarbeyaClassId);
            if (classObj?.Stage == null) return Ok(new { success = true, trips = new List<object>() });
            
            query = query.Where(t => t.FamilyId == classObj.Stage.FamilyId);
        }
        else if (user.Role != "Admin" && user.Role != "TarbeyaGeneralAdmin")
        {
            return Forbid();
        }

        var trips = await query.OrderByDescending(t => t.TripDate).Select(t => new {
            t.Id,
            t.Name,
            t.TripDate,
            t.TicketPrice,
            FamilyName = t.Family != null ? t.Family.Name : ""
        }).ToListAsync();

        return Ok(new { success = true, trips });
    }

    [HttpPost]
    public async Task<IActionResult> CreateTrip([FromBody] TarbeyaTrip dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null || user.Role != "TarbeyaFamilyAdmin") return Forbid();
        if (user.TarbeyaFamilyId == null) return BadRequest("Family Admin does not belong to a family.");

        if (string.IsNullOrWhiteSpace(dto.Name) || dto.TicketPrice < 0)
            return BadRequest("Name and valid TicketPrice are required.");

        var trip = new TarbeyaTrip
        {
            Name = dto.Name,
            TripDate = dto.TripDate,
            TicketPrice = dto.TicketPrice,
            FamilyId = user.TarbeyaFamilyId.Value
        };

        _context.TarbeyaTrips.Add(trip);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, trip = new { trip.Id, trip.Name } });
    }

    [HttpGet("{id}/dashboard")]
    public async Task<IActionResult> GetTripDashboard(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var trip = await _context.TarbeyaTrips
            .Include(t => t.Subscriptions)
            .ThenInclude(s => s.Student)
            .Include(t => t.Subscriptions)
            .ThenInclude(s => s.Servant)
            .Include(t => t.Expenses)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trip == null) return NotFound();

        // Authorization check
        if (user.Role == "TarbeyaFamilyAdmin")
        {
            if (trip.FamilyId != user.TarbeyaFamilyId) return Forbid();
        }
        else if (user.Role == "TarbeyaServant")
        {
            // Servant should only see their own students' subscriptions
            var classObj = await _context.TarbeyaClasses.Include(c => c.Stage).FirstOrDefaultAsync(c => c.Id == user.TarbeyaClassId);
            if (classObj?.Stage == null || trip.FamilyId != classObj.Stage.FamilyId) return Forbid();
        }
        else if (user.Role != "Admin" && user.Role != "TarbeyaGeneralAdmin")
        {
            return Forbid();
        }

        var totalCollected = trip.Subscriptions.Sum(s => s.AmountPaid);
        var totalExpenses = trip.Expenses.Sum(e => e.Amount);
        
        object result = null;

        if (user.Role == "TarbeyaServant")
        {
            // Servants only see their own recorded subscriptions, not expenses
            result = new {
                trip.Id,
                trip.Name,
                trip.TicketPrice,
                Subscriptions = trip.Subscriptions.Where(s => s.ServantId == user.Id).Select(s => new {
                    s.Id,
                    StudentName = s.Student?.Name,
                    s.AmountPaid,
                    s.RegistrationDate
                })
            };
        }
        else
        {
            // Admins see full financial dashboard
            result = new {
                trip.Id,
                trip.Name,
                trip.TripDate,
                trip.TicketPrice,
                TotalCollected = totalCollected,
                TotalExpenses = totalExpenses,
                NetBalance = totalCollected - totalExpenses,
                Expenses = trip.Expenses.OrderByDescending(e => e.ExpenseDate).Select(e => new {
                    e.Id,
                    e.ItemDescription,
                    e.Amount,
                    e.ExpenseDate
                }),
                Subscriptions = trip.Subscriptions.OrderByDescending(s => s.RegistrationDate).Select(s => new {
                    s.Id,
                    StudentName = s.Student?.Name,
                    ServantName = s.Servant?.Username,
                    s.AmountPaid,
                    s.RegistrationDate
                })
            };
        }

        return Ok(new { success = true, dashboard = result });
    }

    [HttpPost("{id}/expenses")]
    public async Task<IActionResult> AddExpense(int id, [FromBody] TarbeyaTripExpense dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null || user.Role != "TarbeyaFamilyAdmin") return Forbid();

        var trip = await _context.TarbeyaTrips.FindAsync(id);
        if (trip == null || trip.FamilyId != user.TarbeyaFamilyId) return NotFound("Trip not found or unauthorized.");

        if (dto.Amount <= 0 || string.IsNullOrWhiteSpace(dto.ItemDescription))
            return BadRequest("Amount and Description are required.");

        var expense = new TarbeyaTripExpense
        {
            TripId = id,
            ItemDescription = dto.ItemDescription,
            Amount = dto.Amount,
            ExpenseDate = dto.ExpenseDate == default ? DateTime.Now : dto.ExpenseDate,
            AddedByFamilyAdminId = user.Id
        };

        _context.TarbeyaTripExpenses.Add(expense);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost("{id}/subscriptions")]
    public async Task<IActionResult> AddSubscription(int id, [FromBody] TarbeyaTripSubscription dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null || (user.Role != "TarbeyaServant" && user.Role != "TarbeyaFamilyAdmin")) return Forbid();

        var trip = await _context.TarbeyaTrips.FindAsync(id);
        if (trip == null) return NotFound("Trip not found.");

        var student = await _context.TarbeyaStudents.Include(s => s.Class).ThenInclude(c => c!.Stage).FirstOrDefaultAsync(s => s.Id == dto.StudentId);
        if (student == null) return NotFound("Student not found.");

        if (user.Role == "TarbeyaServant")
        {
            if (student.ClassId != user.TarbeyaClassId) return Forbid("Student is not in your class.");
            if (student.Class?.Stage?.FamilyId != trip.FamilyId) return Forbid("Student is not in the trip's family.");
        }
        else if (user.Role == "TarbeyaFamilyAdmin")
        {
            if (student.Class?.Stage?.FamilyId != user.TarbeyaFamilyId || trip.FamilyId != user.TarbeyaFamilyId) 
                return Forbid("Unauthorized to add for this student or trip.");
        }

        // Check if already subscribed
        var existing = await _context.TarbeyaTripSubscriptions.FirstOrDefaultAsync(s => s.TripId == id && s.StudentId == dto.StudentId);
        if (existing != null)
        {
            // Update paid amount
            existing.AmountPaid += dto.AmountPaid;
        }
        else
        {
            var sub = new TarbeyaTripSubscription
            {
                TripId = id,
                StudentId = dto.StudentId,
                AmountPaid = dto.AmountPaid,
                RegistrationDate = DateTime.Now,
                ServantId = user.Id
            };
            _context.TarbeyaTripSubscriptions.Add(sub);
        }
        
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
