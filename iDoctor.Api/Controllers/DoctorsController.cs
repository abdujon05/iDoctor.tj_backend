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
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public DoctorsController(ApplicationDbContext db) => _db = db;

    // GET /api/doctors?q=&specialtyId=&clinicId=&cityId=&minExperience=&maxPrice=&sortBy=&page=&pageSize=
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResult<DoctorListItemDto>>> Search([FromQuery] DoctorSearchQuery query)
    {
        var doctors = _db.Doctors.AsNoTracking()
            .Where(d => d.IsVerified && d.User.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = query.Q.ToContainsPattern();
            doctors = doctors.Where(d => EF.Functions.ILike(d.User.FullName, pattern));
        }
        if (query.SpecialtyId.HasValue)
            doctors = doctors.Where(d => d.Specialties.Any(s => s.Id == query.SpecialtyId));
        if (query.ClinicId.HasValue)
            doctors = doctors.Where(d => d.Clinics.Any(c => c.Id == query.ClinicId && c.IsActive));
        if (query.CityId.HasValue)
            doctors = doctors.Where(d => d.Clinics.Any(c => c.CityId == query.CityId && c.IsActive));
        if (query.MinExperience.HasValue)
            doctors = doctors.Where(d => d.ExperienceYears >= query.MinExperience);
        if (query.MaxPrice.HasValue)
            doctors = doctors.Where(d => d.ConsultationPrice != null && d.ConsultationPrice <= query.MaxPrice);

        // UserId as the last tiebreaker keeps page boundaries stable
        var ordered = query.SortBy switch
        {
            "experience" => doctors.OrderByDescending(d => d.ExperienceYears)
                                   .ThenBy(d => d.User.FullName).ThenBy(d => d.UserId),
            "price_asc"  => doctors.OrderBy(d => d.ConsultationPrice)          // nulls sort last
                                   .ThenBy(d => d.User.FullName).ThenBy(d => d.UserId),
            "price_desc" => doctors.OrderByDescending(d => d.ConsultationPrice.HasValue)
                                   .ThenByDescending(d => d.ConsultationPrice)
                                   .ThenBy(d => d.User.FullName).ThenBy(d => d.UserId),
            "name"       => doctors.OrderBy(d => d.User.FullName).ThenBy(d => d.UserId),
            _            => doctors.OrderByDescending(d => d.AverageRating)
                                   .ThenByDescending(d => d.ExperienceYears)
                                   .ThenBy(d => d.User.FullName).ThenBy(d => d.UserId)
        };

        var total = await ordered.CountAsync();

        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new DoctorListItemDto
            {
                Id = d.UserId,
                FullName = d.User.FullName,
                PhotoUrl = d.PhotoUrl,
                ExperienceYears = d.ExperienceYears,
                ConsultationPrice = d.ConsultationPrice,
                AverageRating = d.AverageRating,
                Specialties = d.Specialties.Select(s => new SpecialtyDto(s.Id, s.Name)).ToList(),
                Clinics = d.Clinics.Where(c => c.IsActive)
                                   .Select(c => new ClinicSummaryDto(c.Id, c.Name, c.Address)).ToList()
            })
            .ToListAsync();

        return Ok(new PagedResult<DoctorListItemDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        });
    }

    // Public: only verified, active doctors are visible
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DoctorProfileDto>> GetById(Guid id)
    {
        var doctor = await DoctorQuery()
            .FirstOrDefaultAsync(d => d.UserId == id && d.IsVerified && d.User.IsActive);
        return doctor is null ? NotFound() : Ok(ToDto(doctor));
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpGet("me")]
    public async Task<ActionResult<DoctorProfileDto>> GetMe()
    {
        var doctor = await DoctorQuery().FirstOrDefaultAsync(d => d.UserId == User.GetUserId());
        return doctor is null ? NotFound() : Ok(ToDto(doctor));
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpPut("me")]
    public async Task<ActionResult<DoctorProfileDto>> UpdateMe(UpdateDoctorProfileRequest request)
    {
        var specialtyIds = request.SpecialtyIds.Distinct().ToList();
        var clinicIds = request.ClinicIds.Distinct().ToList();

        var specialties = await _db.Specialties.Where(s => specialtyIds.Contains(s.Id)).ToListAsync();
        if (specialties.Count != specialtyIds.Count)
            return BadRequest(new { error = "One or more specialtyIds are invalid." });

        var clinics = await _db.Clinics.Where(c => clinicIds.Contains(c.Id) && c.IsActive).ToListAsync();
        if (clinics.Count != clinicIds.Count)
            return BadRequest(new { error = "One or more clinicIds are invalid." });

        var doctor = await DoctorQuery().FirstOrDefaultAsync(d => d.UserId == User.GetUserId());
        if (doctor is null) return NotFound();

        doctor.User.FullName = request.FullName;
        doctor.User.UpdatedAt = DateTime.UtcNow;
        doctor.Bio = request.Bio;
        doctor.ExperienceYears = request.ExperienceYears;
        doctor.ConsultationPrice = request.ConsultationPrice;
        doctor.PhotoUrl = request.PhotoUrl;

        doctor.Specialties.Clear();
        foreach (var s in specialties) doctor.Specialties.Add(s);
        doctor.Clinics.Clear();
        foreach (var c in clinics) doctor.Clinics.Add(c);

        await _db.SaveChangesAsync();
        return Ok(ToDto(doctor));
    }

    private IQueryable<Doctor> DoctorQuery() =>
        _db.Doctors.Include(d => d.User).Include(d => d.Specialties).Include(d => d.Clinics);

    private static DoctorProfileDto ToDto(Doctor d) => new()
    {
        Id = d.UserId,
        FullName = d.User.FullName,
        Bio = d.Bio,
        ExperienceYears = d.ExperienceYears,
        ConsultationPrice = d.ConsultationPrice,
        PhotoUrl = d.PhotoUrl,
        IsVerified = d.IsVerified,
        AverageRating = d.AverageRating,
        Specialties = d.Specialties.Select(s => new SpecialtyDto(s.Id, s.Name)).ToList(),
        Clinics = d.Clinics.Select(c => new ClinicSummaryDto(c.Id, c.Name, c.Address)).ToList()
    };
}