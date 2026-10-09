using iDoctor.Api.Dtos;
using iDoctor.Domain.Constants;
using iDoctor.Domain.Entities;
using iDoctor.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iDoctor.Api.Controllers;

[ApiController]
[Route("api/specialties")]
public class SpecialtiesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SpecialtiesController(ApplicationDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<SpecialtyDetailDto>>> GetAll() =>
        Ok(await Project(_db.Specialties.AsNoTracking().OrderBy(s => s.Name)).ToListAsync());

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SpecialtyDetailDto>> GetById(int id)
    {
        var dto = await Project(_db.Specialties.AsNoTracking().Where(s => s.Id == id)).FirstOrDefaultAsync();
        return dto is null ? NotFound() : Ok(dto);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<ActionResult<SpecialtyDetailDto>> Create(SpecialtyRequest request)
    {
        if (await NameTakenAsync(request.Name, excludeId: null))
            return Conflict(new { error = "A specialty with this name already exists." });

        var specialty = new Specialty
        {
            Name = request.Name.Trim(),
            NameRu = request.NameRu?.Trim(),
            NameTj = request.NameTj?.Trim(),
            Description = request.Description?.Trim()
        };
        _db.Specialties.Add(specialty);
        await _db.SaveChangesAsync();

        var dto = await Project(_db.Specialties.AsNoTracking().Where(s => s.Id == specialty.Id)).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = specialty.Id }, dto);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SpecialtyDetailDto>> Update(int id, SpecialtyRequest request)
    {
        var specialty = await _db.Specialties.FirstOrDefaultAsync(s => s.Id == id);
        if (specialty is null) return NotFound();

        if (await NameTakenAsync(request.Name, excludeId: id))
            return Conflict(new { error = "A specialty with this name already exists." });

        specialty.Name = request.Name.Trim();
        specialty.NameRu = request.NameRu?.Trim();
        specialty.NameTj = request.NameTj?.Trim();
        specialty.Description = request.Description?.Trim();
        await _db.SaveChangesAsync();

        return Ok(await Project(_db.Specialties.AsNoTracking().Where(s => s.Id == id)).FirstAsync());
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var specialty = await _db.Specialties.FirstOrDefaultAsync(s => s.Id == id);
        if (specialty is null) return NotFound();

        if (await _db.Doctors.AnyAsync(d => d.Specialties.Any(s => s.Id == id)))
            return Conflict(new { error = "Specialty is assigned to doctors and cannot be deleted." });

        _db.Specialties.Remove(specialty);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private Task<bool> NameTakenAsync(string name, int? excludeId)
    {
        var lowered = name.Trim().ToLower();
        return _db.Specialties.AnyAsync(s => s.Name.ToLower() == lowered && s.Id != excludeId);
    }

    // DoctorsCount only counts doctors visible to the public
    private static IQueryable<SpecialtyDetailDto> Project(IQueryable<Specialty> query) =>
        query.Select(s => new SpecialtyDetailDto(
            s.Id, s.Name, s.NameRu, s.NameTj, s.Description,
            s.Doctors.Count(d => d.IsVerified && d.User.IsActive)));
}