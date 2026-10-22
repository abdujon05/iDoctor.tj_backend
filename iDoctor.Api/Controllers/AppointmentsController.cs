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
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AppointmentsController(ApplicationDbContext db) => _db = db;

    // ================= BOOK =================

    // POST /api/appointments   { "slotId": 12, "reason": "Chest pain" }
    [Authorize(Roles = Roles.Patient)]
    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Book(BookAppointmentRequest request)
    {
        var patientId = User.GetUserId();

        var slot = await _db.AppointmentSlots
            .Include(s => s.Doctor).ThenInclude(d => d.User)
            .Include(s => s.Clinic)
            .FirstOrDefaultAsync(s => s.Id == request.SlotId);

        // ---- friendly pre-checks (the database enforces the same rules below) ----
        if (slot is null)
            return NotFound(new { error = "Slot not found." });

        if (!slot.Doctor.IsVerified || !slot.Doctor.User.IsActive || !slot.Clinic.IsActive)
            return Conflict(new { error = "This doctor or clinic is not currently accepting appointments." });

        if (AppointmentRules.StartOf(slot) <= TajikTime.Now)
            return Conflict(new { error = "This time slot has already started or passed." });

        if (slot.Status != SlotStatus.Available)
            return Conflict(new { error = "This time slot is no longer available." });

        var overlaps = await _db.Appointments.AnyAsync(a =>
            a.PatientId == patientId
            && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            && a.Slot.SlotDate == slot.SlotDate
            && a.Slot.StartTime < slot.EndTime
            && slot.StartTime < a.Slot.EndTime);
        if (overlaps)
            return Conflict(new { error = "You already have an appointment that overlaps with this time." });

        // ---- atomic claim ----
        await using var tx = await _db.Database.BeginTransactionAsync();

        // UPDATE ... WHERE Status = 'Available'. If two patients race for the same slot,
        // Postgres lets one UPDATE through; the other finds 0 matching rows.
        var claimed = await _db.AppointmentSlots
            .Where(s => s.Id == slot.Id && s.Status == SlotStatus.Available)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, SlotStatus.Booked));
        if (claimed == 0)
            return Conflict(new { error = "This time slot was just booked by someone else." });

        var appointment = new Appointment
        {
            SlotId = slot.Id,
            PatientId = patientId,
            DoctorId = slot.DoctorId,
            ClinicId = slot.ClinicId,
            Status = AppointmentStatus.Confirmed,
            Reason = request.Reason?.Trim()
        };
        _db.Appointments.Add(appointment);

        try
        {
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Last line of defense: the partial unique index on appointments.SlotId
            return Conflict(new { error = "This time slot was just booked by someone else." });
        }

        var dto = await LoadDtoAsync(appointment.Id, viewerIsDoctor: false);
        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, dto);
    }

    // ================= CANCEL =================

    // POST /api/appointments/{id}/cancel   { "reason": "Can't make it" }
    [Authorize(Roles = $"{Roles.Patient},{Roles.Doctor}")]
    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancelAppointmentRequest request)
    {
        var userId = User.GetUserId();
        var isDoctor = User.IsInRole(Roles.Doctor);

        var query = _db.Appointments.Include(a => a.Slot).AsQueryable();
        query = isDoctor ? query.Where(a => a.DoctorId == userId) : query.Where(a => a.PatientId == userId);

        var appointment = await query.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null) return NotFound();

        if (!AppointmentRules.IsActive(appointment.Status))
            return Conflict(new { error = "Only pending or confirmed appointments can be cancelled." });

        var start = AppointmentRules.StartOf(appointment.Slot);
        var now = TajikTime.Now;
        if (start <= now)
            return Conflict(new { error = "This appointment has already started or passed." });
        if (!isDoctor && start - now < TimeSpan.FromHours(AppointmentRules.MinPatientCancelNoticeHours))
            return Conflict(new { error = $"Appointments can only be cancelled at least {AppointmentRules.MinPatientCancelNoticeHours} hours in advance." });

        var newStatus = isDoctor ? AppointmentStatus.CancelledByDoctor : AppointmentStatus.CancelledByPatient;
        var reason = request.Reason?.Trim();
        // patient cancels -> slot is free again; doctor cancels -> doctor is unavailable, so block it
        var slotStatus = isDoctor ? SlotStatus.Blocked : SlotStatus.Available;

        await using var tx = await _db.Database.BeginTransactionAsync();

        // Atomic: guards against two simultaneous cancel requests
        var updated = await _db.Appointments
            .Where(a => a.Id == id
                     && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Status, newStatus)
                .SetProperty(a => a.CancelReason, reason)
                .SetProperty(a => a.UpdatedAt, DateTime.UtcNow));
        if (updated == 0)
            return Conflict(new { error = "This appointment was already cancelled." });

        await _db.AppointmentSlots
            .Where(s => s.Id == appointment.SlotId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, slotStatus));

        await tx.CommitAsync();
        return NoContent();
    }

    // ================= READ =================

    // GET /api/appointments?status=Confirmed&from=2026-10-20&to=2026-10-31
    // Patients see their own appointments, doctors see the ones booked with them.
    [Authorize(Roles = $"{Roles.Patient},{Roles.Doctor}")]
    [HttpGet]
    public async Task<ActionResult<List<AppointmentDto>>> GetMine([FromQuery] AppointmentListQuery q)
    {
        var userId = User.GetUserId();
        var isDoctor = User.IsInRole(Roles.Doctor);

        var query = BaseQuery();
        query = isDoctor ? query.Where(a => a.DoctorId == userId) : query.Where(a => a.PatientId == userId);

        if (q.Status.HasValue) query = query.Where(a => a.Status == q.Status.Value);
        if (q.From.HasValue) query = query.Where(a => a.Slot.SlotDate >= q.From.Value);
        if (q.To.HasValue) query = query.Where(a => a.Slot.SlotDate <= q.To.Value);

        var list = await query
            .OrderBy(a => a.Slot.SlotDate).ThenBy(a => a.Slot.StartTime)
            .ToListAsync();

        return Ok(list.Select(a => ToDto(a, isDoctor)).ToList());
    }

    [Authorize(Roles = $"{Roles.Patient},{Roles.Doctor},{Roles.Admin}")]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<AppointmentDto>> GetById(long id)
    {
        var userId = User.GetUserId();
        var isDoctor = User.IsInRole(Roles.Doctor);

        var query = BaseQuery().Where(a => a.Id == id);
        if (isDoctor) query = query.Where(a => a.DoctorId == userId);
        else if (User.IsInRole(Roles.Patient)) query = query.Where(a => a.PatientId == userId);
        // Admin: no extra filter

        var appointment = await query.FirstOrDefaultAsync();
        return appointment is null ? NotFound() : Ok(ToDto(appointment, isDoctor));
    }

    // ================= helpers =================

    private IQueryable<Appointment> BaseQuery() =>
        _db.Appointments.AsNoTracking()
            .Include(a => a.Slot)
            .Include(a => a.Clinic)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Patient).ThenInclude(p => p.User);

    private async Task<AppointmentDto> LoadDtoAsync(long id, bool viewerIsDoctor) =>
        ToDto(await BaseQuery().FirstAsync(a => a.Id == id), viewerIsDoctor);

    private static AppointmentDto ToDto(Appointment a, bool viewerIsDoctor)
    {
        var start = AppointmentRules.StartOf(a.Slot);
        var now = TajikTime.Now;
        var canCancel = AppointmentRules.IsActive(a.Status)
                        && start > now
                        && (viewerIsDoctor
                            || start - now >= TimeSpan.FromHours(AppointmentRules.MinPatientCancelNoticeHours));

        return new AppointmentDto
        {
            Id = a.Id,
            Status = a.Status,
            Date = a.Slot.SlotDate,
            StartTime = a.Slot.StartTime,
            EndTime = a.Slot.EndTime,
            Reason = a.Reason,
            CancelReason = a.CancelReason,
            CreatedAt = a.CreatedAt,
            CanCancel = canCancel,
            Doctor = new DoctorBriefDto(a.DoctorId, a.Doctor.User.FullName, a.Doctor.PhotoUrl),
            Patient = new PatientBriefDto(a.PatientId, a.Patient.User.FullName, a.Patient.User.PhoneNumber!),
            Clinic = new ClinicSummaryDto(a.Clinic.Id, a.Clinic.Name, a.Clinic.Address)
        };
    }
}