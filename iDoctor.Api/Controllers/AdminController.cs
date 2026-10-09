using iDoctor.Api.Dtos;
using iDoctor.Api.Extensions;
using iDoctor.Domain.Constants;
using iDoctor.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iDoctor.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AdminController(ApplicationDbContext db) => _db = db;

    // GET /api/admin/doctors?verified=false&q=ali   -> pending approvals, name/email search
    [HttpGet("doctors")]
    public async Task<ActionResult<List<AdminDoctorDto>>> GetDoctors(
        [FromQuery] bool? verified, [FromQuery] string? q)
    {
        var query = _db.Doctors.AsNoTracking().AsQueryable();
        if (verified.HasValue) query = query.Where(d => d.IsVerified == verified.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = q.ToContainsPattern();
            query = query.Where(d => EF.Functions.ILike(d.User.FullName, pattern)
                                  || EF.Functions.ILike(d.User.Email!, pattern));
        }

        var list = await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new AdminDoctorDto(d.UserId, d.User.FullName, d.User.Email!,
                                            d.User.PhoneNumber!, d.IsVerified, d.CreatedAt))
            .ToListAsync();
        return Ok(list);
    }

    [HttpPut("doctors/{id:guid}/verification")]
    public async Task<IActionResult> SetVerification(Guid id, SetVerificationRequest request)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == id);
        if (doctor is null) return NotFound();

        doctor.IsVerified = request.IsVerified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Deactivated doctors disappear from search and cannot log in
    [HttpPut("doctors/{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, SetActiveRequest request)
    {
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.UserId == id);
        if (doctor is null) return NotFound();

        doctor.User.IsActive = request.IsActive;
        doctor.User.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}