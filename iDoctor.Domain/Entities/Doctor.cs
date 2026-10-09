namespace iDoctor.Domain.Entities;

public class Doctor
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? Bio { get; set; }
    public short ExperienceYears { get; set; }
    public decimal? ConsultationPrice { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsVerified { get; set; } = false;
    public decimal AverageRating { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Specialty> Specialties { get; set; } = new List<Specialty>();
    public ICollection<Clinic> Clinics { get; set; } = new List<Clinic>();
    public ICollection<DoctorWorkingHours> WorkingHours { get; set; } = new List<DoctorWorkingHours>();
    public ICollection<AppointmentSlot> Slots { get; set; } = new List<AppointmentSlot>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}