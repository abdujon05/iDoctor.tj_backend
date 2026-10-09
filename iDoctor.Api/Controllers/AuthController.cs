using iDoctor.Api.Dtos;
using iDoctor.Api.Extensions;
using iDoctor.Api.Services;
using iDoctor.Domain.Constants;
using iDoctor.Domain.Entities;
using iDoctor.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iDoctor.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IJwtTokenService _tokens;

    public AuthController(UserManager<User> userManager, ApplicationDbContext db, IJwtTokenService tokens)
    {
        _userManager = userManager;
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("register/patient")]
    public async Task<ActionResult<AuthResponse>> RegisterPatient(RegisterPatientRequest request)
    {
        if (request.CityId.HasValue && !await _db.Cities.AnyAsync(c => c.Id == request.CityId))
            return BadRequest(new { error = "Invalid cityId." });

        await using var tx = await _db.Database.BeginTransactionAsync();

        var (user, error) = await CreateUserAsync(request, Roles.Patient);
        if (error is not null) return error;

        _db.Patients.Add(new Patient
        {
            UserId = user!.Id,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            CityId = request.CityId
        });
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(await BuildAuthResponseAsync(user));
    }

    [HttpPost("register/doctor")]
    public async Task<ActionResult<AuthResponse>> RegisterDoctor(RegisterDoctorRequest request)
    {
        var distinctIds = request.SpecialtyIds.Distinct().ToList();
        var specialties = await _db.Specialties.Where(s => distinctIds.Contains(s.Id)).ToListAsync();
        if (specialties.Count != distinctIds.Count)
            return BadRequest(new { error = "One or more specialtyIds are invalid." });

        await using var tx = await _db.Database.BeginTransactionAsync();

        var (user, error) = await CreateUserAsync(request, Roles.Doctor);
        if (error is not null) return error;

        _db.Doctors.Add(new Doctor
        {
            UserId = user!.Id,
            Bio = request.Bio,
            ExperienceYears = request.ExperienceYears,
            ConsultationPrice = request.ConsultationPrice,
            IsVerified = false,           // admin must approve
            Specialties = specialties
        });
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(await BuildAuthResponseAsync(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return Unauthorized(new { error = "Invalid email or password." });

        if (await _userManager.IsLockedOutAsync(user))
            return Unauthorized(new { error = "Account temporarily locked. Try again later." });

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            return Unauthorized(new { error = "Invalid email or password." });
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return Ok(await BuildAuthResponseAsync(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserInfo>> Me()
    {
        var user = await _userManager.FindByIdAsync(User.GetUserId().ToString());
        if (user is null || !user.IsActive) return Unauthorized();

        return Ok(new UserInfo
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Roles = await _userManager.GetRolesAsync(user)
        });
    }

    // ---- helpers ----

    private async Task<(User? User, ActionResult? Error)> CreateUserAsync(RegisterRequestBase request, string role)
    {
        if (await _userManager.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber))
            return (null, Conflict(new { error = "Phone number is already registered." }));

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            FullName = request.FullName
        };

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
            return (null, BadRequest(new { errors = create.Errors.Select(e => new { e.Code, e.Description }) }));

        await _userManager.AddToRoleAsync(user, role);
        return (user, null);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expires) = _tokens.CreateToken(user, roles);
        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expires,
            User = new UserInfo { Id = user.Id, FullName = user.FullName, Email = user.Email!, Roles = roles }
        };
    }
}