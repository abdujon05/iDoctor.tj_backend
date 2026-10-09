namespace iDoctor.Domain.Entities;

public class Appointment
{
    public long Id { get; set; }

    public long SlotId { get; set; }
    public AppointmentSlot Slot { get; set; } = null!;

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Reason { get; set; }
    public string? CancelReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}