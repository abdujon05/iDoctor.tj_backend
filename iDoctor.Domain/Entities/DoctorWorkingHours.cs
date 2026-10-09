namespace iDoctor.Domain.Entities;

public class DoctorWorkingHours
{
    public int Id { get; set; }

    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public short DayOfWeek { get; set; } // 0=Sunday ... 6=Saturday
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public short SlotDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}