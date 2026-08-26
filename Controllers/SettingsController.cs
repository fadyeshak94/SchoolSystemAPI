using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public SettingsController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet]
    [AllowAnonymous] // للسماح بجلب رقم الواتساب بدون تسجيل دخول
    public async Task<IActionResult> GetSettings()
    {
        var settings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        if (settings == null) return Ok(new AppSetting()); // Default values
        return Ok(settings);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSettings([FromBody] AppSetting dto)
    {
        var settings = (await _uow.AppSettings.FindAsync(s => true)).FirstOrDefault();
        if (settings == null)
        {
            await _uow.AppSettings.AddAsync(dto);
        }
        else
        {
            settings.AcademicYear = dto.AcademicYear;
            settings.CurrentTerm = dto.CurrentTerm;
            settings.AdminWhatsapp = dto.AdminWhatsapp;
            _uow.AppSettings.Update(settings);
        }

        await _uow.CompleteAsync();
        return Ok(new { success = true, message = "تم حفظ الإعدادات بنجاح" });
    }

    [HttpGet("fees")]
    public async Task<IActionResult> GetFees()
    {
        var fees = await _uow.StageFees.FindAsync(f => true);
        var dict = fees.ToDictionary(f => f.StageName, f => f.FeeAmount);
        return Ok(dict);
    }

    [HttpPost("fees")]
    public async Task<IActionResult> SaveFees([FromBody] Dictionary<string, decimal> feesDto)
    {
        var existingFees = await _uow.StageFees.FindAsync(f => true);

        foreach (var fee in feesDto)
        {
            var stageFee = existingFees.FirstOrDefault(f => f.StageName == fee.Key);
            if (stageFee != null)
            {
                stageFee.FeeAmount = fee.Value;
                _uow.StageFees.Update(stageFee);
            }
            else
            {
                await _uow.StageFees.AddAsync(new StageFee { StageName = fee.Key, FeeAmount = fee.Value });
            }
        }

        await _uow.CompleteAsync();
        return Ok(new { success = true, message = "تم حفظ المصروفات بنجاح" });
    }
}
