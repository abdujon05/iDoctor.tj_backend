namespace iDoctor.Domain.Entities;

public class City
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<Clinic> Clinics { get; set; } = new List<Clinic>();
    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
}