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
public class TarbeyaAreasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TarbeyaAreasController(ApplicationDbContext context)
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
    public async Task<IActionResult> GetAreas()
    {
        var areas = await _context.TarbeyaAreas.OrderBy(a => a.Name).ToListAsync();
        return Ok(new { success = true, areas });
    }

    [HttpPost]
    public async Task<IActionResult> CreateArea([FromBody] TarbeyaArea dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null || (user.Role != "Admin" && user.Role != "TarbeyaGeneralAdmin")) 
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");

        _context.TarbeyaAreas.Add(new TarbeyaArea { Name = dto.Name.Trim() });
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateArea(int id, [FromBody] TarbeyaArea dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null || (user.Role != "Admin" && user.Role != "TarbeyaGeneralAdmin")) 
            return Forbid();

        var area = await _context.TarbeyaAreas.FindAsync(id);
        if (area == null) return NotFound();

        area.Name = dto.Name.Trim();
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteArea(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null || (user.Role != "Admin" && user.Role != "TarbeyaGeneralAdmin")) 
            return Forbid();

        var area = await _context.TarbeyaAreas.FindAsync(id);
        if (area == null) return NotFound();

        // Check if there are students in this area
        bool hasStudents = await _context.TarbeyaStudents.AnyAsync(s => s.AreaId == id);
        if (hasStudents)
            return BadRequest(new { success = false, message = "لا يمكن حذف المنطقة لوجود مخدومين مسجلين بها." });

        _context.TarbeyaAreas.Remove(area);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
