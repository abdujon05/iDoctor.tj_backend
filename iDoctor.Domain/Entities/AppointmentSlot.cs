namespace iDoctor.Domain.Entities;

public class AppointmentSlot
{
    public long Id { get; set; }

    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public DateOnly SlotDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public SlotStatus Status { get; set; } = SlotStatus.Available;

    public Appointment? Appointment { get; set; }
}