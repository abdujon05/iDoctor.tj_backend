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
[Route("api/patients")]
[Authorize(Roles = Roles.Patient)]
public class PatientsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PatientsController(ApplicationDbContext db) => _db = db;

    [HttpGet("me")]
    public async Task<ActionResult<PatientProfileDto>> GetMe()
    {
        var patient = await LoadPatientAsync(User.GetUserId());
        return patient is null ? NotFound() : Ok(ToDto(patient));
    }

    [HttpPut("me")]
    public async Task<ActionResult<PatientProfileDto>> UpdateMe(UpdatePatientProfileRequest request)
    {
        if (request.CityId.HasValue && !await _db.Cities.AnyAsync(c => c.Id == request.CityId))
            return BadRequest(new { error = "Invalid cityId." });

        var patient = await LoadPatientAsync(User.GetUserId());
        if (patient is null) return NotFound();

        patient.User.FullName = request.FullName;
        patient.User.UpdatedAt = DateTime.UtcNow;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.CityId = request.CityId;
        await _db.SaveChangesAsync();

        patient = await LoadPatientAsync(patient.UserId); // reload so City is fresh
        return Ok(ToDto(patient!));
    }

    private Task<Patient?> LoadPatientAsync(Guid id) =>
        _db.Patients.Include(p => p.User).Include(p => p.City)
            .FirstOrDefaultAsync(p => p.UserId == id);

    private static PatientProfileDto ToDto(Patient p) => new()
    {
        Id = p.UserId,
        FullName = p.User.FullName,
        Email = p.User.Email!,
        PhoneNumber = p.User.PhoneNumber!,
        DateOfBirth = p.DateOfBirth,
        Gender = p.Gender,
        CityId = p.CityId,
        CityName = p.City?.Name
    };
}