using System.ComponentModel.DataAnnotations;

namespace iDoctor.Api.Dtos;

public class PatientProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public int? CityId { get; set; }
    public string? CityName { get; set; }
}

public class UpdatePatientProfileRequest
{
    [Required, MaxLength(200)] public string FullName { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    [RegularExpression("^(Male|Female)$")] public string? Gender { get; set; }
    public int? CityId { get; set; }
}

public record SpecialtyDto(int Id, string Name);
public record ClinicSummaryDto(int Id, string Name, string? Address);

public class DoctorProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Bio { get; set; }
    public short ExperienceYears { get; set; }
    public decimal? ConsultationPrice { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsVerified { get; set; }
    public decimal AverageRating { get; set; }
    public List<SpecialtyDto> Specialties { get; set; } = new();
    public List<ClinicSummaryDto> Clinics { get; set; } = new();
}

public class UpdateDoctorProfileRequest
{
    [Required, MaxLength(200)] public string FullName { get; set; } = null!;
    [MaxLength(4000)] public string? Bio { get; set; }
    [Range(0, 70)] public short ExperienceYears { get; set; }
    [Range(0, 100000)] public decimal? ConsultationPrice { get; set; }
    [MaxLength(500)] public string? PhotoUrl { get; set; }
    [Required, MinLength(1)] public List<int> SpecialtyIds { get; set; } = new();
    public List<int> ClinicIds { get; set; } = new();
}

public class SetVerificationRequest
{
    public bool IsVerified { get; set; }
}

public record AdminDoctorDto(Guid Id, string FullName, string Email, string PhoneNumber, bool IsVerified, DateTime CreatedAt);