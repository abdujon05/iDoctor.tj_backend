namespace iDoctor.Domain.Entities;

public class Clinic
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public int? CityId { get; set; }
    public City? City { get; set; }

    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    public ICollection<DoctorWorkingHours> WorkingHours { get; set; } = new List<DoctorWorkingHours>();
    public ICollection<AppointmentSlot> Slots { get; set; } = new List<AppointmentSlot>();
}