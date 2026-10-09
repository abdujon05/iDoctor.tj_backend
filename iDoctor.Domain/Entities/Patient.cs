namespace iDoctor.Domain.Entities;

public class Patient
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }

    public int? CityId { get; set; }
    public City? City { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}