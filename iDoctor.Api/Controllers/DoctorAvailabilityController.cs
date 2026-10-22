using iDoctor.Api.Dtos;
using iDoctor.Api.Extensions;
using iDoctor.Api.Services;
using iDoctor.Domain.Constants;
using iDoctor.Domain.Entities;
using iDoctor.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace iDoctor.Api.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorAvailabilityController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public DoctorAvailabilityController(ApplicationDbContext db) => _db = db;

    // ================= PUBLIC: bookable slots =================

    // GET /api/doctors/{id}/slots?from=2026-10-20&to=2026-10-27&clinicId=1
    [AllowAnonymous]
    [HttpGet("{id:guid}/slots")]
    public async Task<ActionResult<List<DaySlotsDto>>> GetAvailableSlots(
        Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? clinicId)
    {
        if (!await _db.Doctors.AnyAsync(d => d.UserId == id && d.IsVerified && d.User.IsActive))
            return NotFound();

        var error = ResolveRange(from, to, clampToToday: true, out var start, out var end);
        if (error is not null) return BadRequest(new { error });

        var today = TajikTime.Today;
        var nowTime = TimeOnly.FromDateTime(TajikTime.Now);

        var query = _db.AppointmentSlots.AsNoTracking()
            .Where(s => s.DoctorId == id
                     && s.Status == SlotStatus.Available
                     && s.Clinic.IsActive
                     && s.SlotDate >= start && s.SlotDate <= end
                     && (s.SlotDate > today || s.StartTime > nowTime));   // hide slots that already started

        if (clinicId.HasValue) query = query.Where(s => s.ClinicId == clinicId.Value);

        var slots = await query
            .OrderBy(s => s.SlotDate).ThenBy(s => s.StartTime)
            .Select(s => new SlotDto(s.Id, s.ClinicId, s.Clinic.Name, s.SlotDate, s.StartTime, s.EndTime))
            .ToListAsync();

        return Ok(slots.GroupBy(s => s.Date)
                       .Select(g => new DaySlotsDto(g.Key, g.ToList()))
                       .ToList());
    }

    // ================= DOCTOR: working hours (weekly template) =================

    [Authorize(Roles = Roles.Doctor)]
    [HttpGet("me/working-hours")]
    public async Task<ActionResult<List<WorkingHoursDto>>> GetWorkingHours()
    {
        var doctorId = User.GetUserId();
        var list = await _db.DoctorWorkingHours.AsNoTracking()
            .Where(w => w.DoctorId == doctorId)
            .OrderBy(w => w.DayOfWeek).ThenBy(w => w.StartTime)
            .Select(w => new WorkingHoursDto(w.Id, w.ClinicId, w.Clinic.Name, w.DayOfWeek,
                w.StartTime, w.EndTime, w.SlotDurationMinutes, w.IsActive))
            .ToListAsync();
        return Ok(list);
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpPost("me/working-hours")]
    public async Task<ActionResult<WorkingHoursDto>> AddWorkingHours(WorkingHoursRequest request)
    {
        var doctorId = User.GetUserId();
        var error = await ValidateWorkingHoursAsync(doctorId, request, excludeId: null);
        if (error is not null) return BadRequest(new { error });

        var hours = new DoctorWorkingHours
        {
            DoctorId = doctorId,
            ClinicId = request.ClinicId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            SlotDurationMinutes = request.SlotDurationMinutes,
            IsActive = request.IsActive
        };
        _db.DoctorWorkingHours.Add(hours);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, await LoadWorkingHoursDtoAsync(hours.Id));
    }

    [Authorize(Roles = Roles.Doctor)]
    [HttpPut("me/working-hours/{id:int}")]
    public async Task<ActionResult<WorkingHoursDto>> UpdateWorkingHours(int id, WorkingHoursRequest request)
    {
        var doctorId = User.GetUserId();
        var hours = await _db.DoctorWorkingHours.FirstOrDefaultAsync(w => w.Id == id && w.DoctorId == doctorId);
        if (hours is null) return NotFound();

        var error = await ValidateWorkingHoursAsync(doctorId, request, excludeId: id);
        if (error is not null) return BadRequest(new { error });

        hours.ClinicId = request.ClinicId;
        hours.DayOfWeek = request.DayOfWeek;
        hours.StartTime = request.StartTime;
        hours.EndTime = request.EndTime;
        hours.SlotDurationMinutes = request.SlotDurationMinutes;
        hours.IsActive = request.IsActive;
        await _db.SaveChangesAsync();

        return Ok(await LoadWorkingHoursDtoAsync(id));
    }

    // Removes the template only. Slots that were already generated stay as they are.
    [Authorize(Roles = Roles.Doctor)]
    [HttpDelete("me/working-hours/{id:int}")]
    public async Task<IActionResult> DeleteWorkingHours(int id)
    {
        var doctorId = User.GetUserId();
        var deleted = await _db.DoctorWorkingHours
            .Where(w => w.Id == id && w.DoctorId == doctorId)
            .ExecuteDeleteAsync();
        return deleted == 0 ? NotFound() : NoContent();
    }

    // ================= DOCTOR: slots =================

    // POST /api/doctors/me/slots/generate   { "fromDate": "2026-10-20", "toDate": "2026-10-27" }
    [Authorize(Roles = Roles.Doctor)]
    [HttpPost("me/slots/generate")]
    public async Task<ActionResult<GenerateSlotsResult>> GenerateSlots(GenerateSlotsRequest request)
    {
        var doctorId = User.GetUserId();
        var today = TajikTime.Today;

        if (request.FromDate < today)
            return BadRequest(new { error = "fromDate cannot be in the past." });
        if (request.ToDate < request.FromDate)
            return BadRequest(new { error = "toDate must not be earlier than fromDate." });
        if (request.ToDate.DayNumber - request.FromDate.DayNumber + 1 > AppointmentRules.MaxRangeDays)
            return BadRequest(new { error = $"You can generate at most {AppointmentRules.MaxRangeDays} days at a time." });

        var templates = await _db.DoctorWorkingHours.AsNoTracking()
            .Where(w => w.DoctorId == doctorId && w.IsActive && w.Clinic.IsActive)
            .ToListAsync();
        if (templates.Count == 0)
            return BadRequest(new { error = "You have no active working hours. Add some first." });

        var existing = await _db.AppointmentSlots.AsNoTracking()
            .Where(s => s.DoctorId == doctorId && s.SlotDate >= request.FromDate && s.SlotDate <= request.ToDate)
            .Select(s => new { s.SlotDate, s.StartTime })
            .ToListAsync();
        var taken = existing.Select(e => (e.SlotDate, e.StartTime)).ToHashSet();

        var now = TajikTime.Now;
        var newSlots = new List<AppointmentSlot>();
        var skipped = 0;

        for (var date = request.FromDate; date <= request.ToDate; date = date.AddDays(1))
        {
            var dow = (short)date.DayOfWeek;
            foreach (var t in templates.Where(w => w.DayOfWeek == dow))
            {
                var step = TimeSpan.FromMinutes(t.SlotDurationMinutes);
                var windowEnd = t.EndTime.ToTimeSpan();

                // TimeSpan math (not TimeOnly) so a slot can never wrap past midnight
                for (var cursor = t.StartTime.ToTimeSpan(); cursor + step <= windowEnd; cursor += step)
                {
                    var start = TimeOnly.FromTimeSpan(cursor);
                    if (date.ToDateTime(start) <= now) continue;             // never create slots in the past
                    if (!taken.Add((date, start))) { skipped++; continue; }  // already exists

                    newSlots.Add(new AppointmentSlot
                    {
                        DoctorId = doctorId,
                        ClinicId = t.ClinicId,
                        SlotDate = date,
                        StartTime = start,
                        EndTime = TimeOnly.FromTimeSpan(cursor + step),
                        Status = SlotStatus.Available
                    });
                }
            }
        }

        if (newSlots.Count > 0)
        {
            _db.AppointmentSlots.AddRange(newSlots);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Conflict(new { error = "Slots were being generated at the same time. Please retry." });
            }
        }

        return Ok(new GenerateSlotsResult(newSlots.Count, skipped, request.FromDate, request.ToDate));
    }

    // GET /api/doctors/me/slots?from=&to=&status=Booked
    [Authorize(Roles = Roles.Doctor)]
    [HttpGet("me/slots")]
    public async Task<ActionResult<List<DoctorSlotDto>>> GetMySlots(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] SlotStatus? status)
    {
        var error = ResolveRange(from, to, clampToToday: false, out var start, out var end);
        if (error is not null) return BadRequest(new { error });

        var doctorId = User.GetUserId();
        var query = _db.AppointmentSlots.AsNoTracking()
            .Where(s => s.DoctorId == doctorId && s.SlotDate >= start && s.SlotDate <= end);
        if (status.HasValue) query = query.Where(s => s.Status == status.Value);

        var list = await query
            .OrderBy(s => s.SlotDate).ThenBy(s => s.StartTime)
            .Select(s => new DoctorSlotDto(s.Id, s.ClinicId, s.Clinic.Name, s.SlotDate,
                s.StartTime, s.EndTime, s.Status))
            .ToListAsync();
        return Ok(list);
    }

    // Block or unblock a free slot (day off, lunch break). Booked slots cannot be blocked.
    [Authorize(Roles = Roles.Doctor)]
    [HttpPut("me/slots/{id:long}/block")]
    public async Task<IActionResult> SetBlocked(long id, BlockSlotRequest request)
    {
        var doctorId = User.GetUserId();
        var expected = request.IsBlocked ? SlotStatus.Available : SlotStatus.Blocked;
        var target = request.IsBlocked ? SlotStatus.Blocked : SlotStatus.Available;

        // Atomic: only flips the slot if nobody booked it in the meantime
        var updated = await _db.AppointmentSlots
            .Where(s => s.Id == id && s.DoctorId == doctorId && s.Status == expected)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, target));
        if (updated > 0) return NoContent();

        var slot = await _db.AppointmentSlots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.DoctorId == doctorId);
        if (slot is null) return NotFound();
        if (slot.Status == SlotStatus.Booked)
            return Conflict(new { error = "This slot has an appointment. Cancel the appointment first." });

        return NoContent();   // already in the requested state
    }

    // ================= helpers =================

    private static string? ResolveRange(DateOnly? from, DateOnly? to, bool clampToToday,
        out DateOnly start, out DateOnly end)
    {
        var today = TajikTime.Today;
        start = from ?? today;
        if (clampToToday && start < today) start = today;
        end = to ?? start.AddDays(13);

        if (end < start) return "'to' must not be earlier than 'from'.";
        if (end.DayNumber - start.DayNumber + 1 > AppointmentRules.MaxRangeDays)
            return $"Date range cannot exceed {AppointmentRules.MaxRangeDays} days.";
        return null;
    }

    private async Task<string?> ValidateWorkingHoursAsync(Guid doctorId, WorkingHoursRequest r, int? excludeId)
    {
        if (r.EndTime <= r.StartTime)
            return "endTime must be later than startTime.";
        if ((r.EndTime - r.StartTime).TotalMinutes < r.SlotDurationMinutes)
            return "The time window is shorter than a single slot.";

        var clinicOk = await _db.Clinics.AnyAsync(c =>
            c.Id == r.ClinicId && c.IsActive && c.Doctors.Any(d => d.UserId == doctorId));
        if (!clinicOk)
            return "Clinic not found, inactive, or not in your profile. Add it through PUT /api/doctors/me first.";

        if (r.IsActive)
        {
            // A doctor cannot be in two places at once, so no overlap on the same day (across all clinics)
            var overlaps = await _db.DoctorWorkingHours.AnyAsync(w =>
                w.DoctorId == doctorId && w.IsActive && w.Id != excludeId
                && w.DayOfWeek == r.DayOfWeek
                && w.StartTime < r.EndTime && r.StartTime < w.EndTime);
            if (overlaps)
                return "These hours overlap with another of your working-hour windows on the same day.";
        }
        return null;
    }

    private Task<WorkingHoursDto> LoadWorkingHoursDtoAsync(int id) =>
        _db.DoctorWorkingHours.AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new WorkingHoursDto(w.Id, w.ClinicId, w.Clinic.Name, w.DayOfWeek,
                w.StartTime, w.EndTime, w.SlotDurationMinutes, w.IsActive))
            .FirstAsync();
}