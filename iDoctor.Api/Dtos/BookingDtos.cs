using System.ComponentModel.DataAnnotations;
using iDoctor.Domain.Entities;

namespace iDoctor.Api.Dtos;

// ---------- Working hours (weekly template) ----------
public class WorkingHoursRequest
{
    [Range(1, int.MaxValue)] public int ClinicId { get; set; }
    [Range(0, 6)] public short DayOfWeek { get; set; }            // 0 = Sunday ... 6 = Saturday
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    [Range(5, 240)] public short SlotDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}

public record WorkingHoursDto(int Id, int ClinicId, string ClinicName, short DayOfWeek,
    TimeOnly StartTime, TimeOnly EndTime, short SlotDurationMinutes, bool IsActive);

// ---------- Slots ----------
public class GenerateSlotsRequest
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

public record GenerateSlotsResult(int Created, int Skipped, DateOnly FromDate, DateOnly ToDate);

public class BlockSlotRequest
{
    public bool IsBlocked { get; set; }
}

public record SlotDto(long Id, int ClinicId, string ClinicName, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime);
public record DaySlotsDto(DateOnly Date, List<SlotDto> Slots);
public record DoctorSlotDto(long Id, int ClinicId, string ClinicName, DateOnly Date,
    TimeOnly StartTime, TimeOnly EndTime, SlotStatus Status);

// ---------- Appointments ----------
public class BookAppointmentRequest
{
    [Range(1, long.MaxValue)] public long SlotId { get; set; }
    [MaxLength(500)] public string? Reason { get; set; }
}

public class CancelAppointmentRequest
{
    [MaxLength(500)] public string? Reason { get; set; }
}

public class AppointmentListQuery
{
    public AppointmentStatus? Status { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public record DoctorBriefDto(Guid Id, string FullName, string? PhotoUrl);
public record PatientBriefDto(Guid Id, string FullName, string PhoneNumber);

public class AppointmentDto
{
    public long Id { get; set; }
    public AppointmentStatus Status { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Reason { get; set; }
    public string? CancelReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool CanCancel { get; set; }
    public DoctorBriefDto Doctor { get; set; } = null!;
    public PatientBriefDto Patient { get; set; } = null!;
    public ClinicSummaryDto Clinic { get; set; } = null!;
}