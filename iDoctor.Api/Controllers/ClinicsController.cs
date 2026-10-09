using iDoctor.Api.Dtos;
using iDoctor.Api.Extensions;
using iDoctor.Domain.Constants;
using iDoctor.Domain.Entities;
using iDoctor.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iDoctor.Api.Controllers;

[ApiController]
[Route("api/clinics")]
public class ClinicsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ClinicsController(ApplicationDbContext db) => _db = db;

    // GET /api/clinics?cityId=1&q=medical        (admins can add &includeInactive=true)
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<ClinicDto>>> GetAll(
        [FromQuery] int? cityId, [FromQuery] string? q, [FromQuery] bool includeInactive = false)
    {
        var query = _db.Clinics.AsNoTracking().AsQueryable();

        // Inactive clinics are visible to admins only
        if (!(includeInactive && User.IsInRole(Roles.Admin)))
            query = query.Where(c => c.IsActive);

        if (cityId.HasValue)
            query = query.Where(c => c.CityId == cityId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = q.ToContainsPattern();
            query = query.Where(c => EF.Functions.ILike(c.Name, pattern));
        }

        return Ok(await Project(query.OrderBy(c => c.Name)).ToListAsync());
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClinicDto>> GetById(int id)
    {
        var isAdmin = User.IsInRole(Roles.Admin);
        var dto = await Project(_db.Clinics.AsNoTracking()
                .Where(c => c.Id == id && (c.IsActive || isAdmin)))
            .FirstOrDefaultAsync();
        return dto is null ? NotFound() : Ok(dto);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<ActionResult<ClinicDto>> Create(ClinicRequest request)
    {
        if (request.CityId.HasValue && !await _db.Cities.AnyAsync(c => c.Id == request.CityId))
            return BadRequest(new { error = "Invalid cityId." });

        var clinic = new Clinic
        {
            Name = request.Name.Trim(),
            CityId = request.CityId,
            Address = request.Address?.Trim(),
            Phone = request.Phone?.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive
        };
        _db.Clinics.Add(clinic);
        await _db.SaveChangesAsync();

        var dto = await Project(_db.Clinics.AsNoTracking().Where(c => c.Id == clinic.Id)).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = clinic.Id }, dto);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClinicDto>> Update(int id, ClinicRequest request)
    {
        var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == id);
        if (clinic is null) return NotFound();

        if (request.CityId.HasValue && !await _db.Cities.AnyAsync(c => c.Id == request.CityId))
            return BadRequest(new { error = "Invalid cityId." });

        clinic.Name = request.Name.Trim();
        clinic.CityId = request.CityId;
        clinic.Address = request.Address?.Trim();
        clinic.Phone = request.Phone?.Trim();
        clinic.Description = request.Description?.Trim();
        clinic.IsActive = request.IsActive;
        await _db.SaveChangesAsync();

        return Ok(await Project(_db.Clinics.AsNoTracking().Where(c => c.Id == id)).FirstAsync());
    }

    // Soft delete: appointments and slots reference clinics, so we deactivate instead of removing the row
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == id);
        if (clinic is null) return NotFound();

        clinic.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static IQueryable<ClinicDto> Project(IQueryable<Clinic> query) =>
        query.Select(c => new ClinicDto(
            c.Id, c.Name, c.CityId, c.City != null ? c.City.Name : null,
            c.Address, c.Phone, c.Description, c.IsActive,
            c.Doctors.Count(d => d.IsVerified && d.User.IsActive)));
}