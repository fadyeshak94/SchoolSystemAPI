using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TarbeyaHierarchyController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TarbeyaHierarchyController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("family")]
    [Authorize(Roles = "TarbeyaGeneralAdmin")]
    public async Task<IActionResult> CreateFamily([FromBody] TarbeyaFamily dto)
    {
        if (string.IsNullOrEmpty(dto.Name)) return BadRequest("Name is required");

        var family = new TarbeyaFamily { Name = dto.Name };
        _context.TarbeyaFamilies.Add(family);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, family });
    }

    [HttpPost("stage")]
    [Authorize(Roles = "TarbeyaGeneralAdmin,TarbeyaFamilyAdmin")]
    public async Task<IActionResult> CreateStage([FromBody] TarbeyaStage dto)
    {
        if (string.IsNullOrEmpty(dto.Name) || dto.FamilyId == 0) return BadRequest("Name and FamilyId are required");
        
        var stage = new TarbeyaStage { Name = dto.Name, FamilyId = dto.FamilyId };
        _context.TarbeyaStages.Add(stage);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, stage });
    }

    [HttpPost("class")]
    [Authorize(Roles = "TarbeyaGeneralAdmin,TarbeyaFamilyAdmin")]
    public async Task<IActionResult> CreateClass([FromBody] TarbeyaClass dto)
    {
        if (string.IsNullOrEmpty(dto.Name) || dto.StageId == 0) return BadRequest("Name and StageId are required");
        
        var newClass = new TarbeyaClass { Name = dto.Name, StageId = dto.StageId };
        _context.TarbeyaClasses.Add(newClass);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, classData = newClass });
    }

    [HttpGet("families")]
    public async Task<IActionResult> GetFamilies()
    {
        var families = await _context.TarbeyaFamilies.Include(f => f.Stages).ThenInclude(s => s.Classes).ToListAsync();
        
        var result = families.Select(f => new {
            f.Id,
            f.Name,
            Stages = f.Stages.Select(s => new {
                s.Id,
                s.Name,
                Classes = s.Classes.Select(c => new {
                    c.Id,
                    c.Name
                })
            })
        });

        return Ok(new { success = true, families = result });
    }
}
