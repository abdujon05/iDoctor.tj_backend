using iDoctor.Api.Dtos;
using iDoctor.Domain.Constants;
using iDoctor.Domain.Entities;
using iDoctor.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iDoctor.Api.Controllers;

[ApiController]
[Route("api/cities")]
public class CitiesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public CitiesController(ApplicationDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<CityDto>>> GetAll() =>
        Ok(await _db.Cities.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new CityDto(c.Id, c.Name)).ToListAsync());

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<ActionResult<CityDto>> Create(CityRequest request)
    {
        var name = request.Name.Trim();
        if (await _db.Cities.AnyAsync(c => c.Name.ToLower() == name.ToLower()))
            return Conflict(new { error = "City already exists." });

        var city = new City { Name = name };
        _db.Cities.Add(city);
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, new CityDto(city.Id, city.Name));
    }
}