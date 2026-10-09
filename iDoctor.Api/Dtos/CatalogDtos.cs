using System.ComponentModel.DataAnnotations;

namespace iDoctor.Api.Dtos;

// ---- Cities ----
public record CityDto(int Id, string Name);

public class CityRequest
{
    [Required, MaxLength(100)] public string Name { get; set; } = null!;
}

// ---- Specialties ----
public record SpecialtyDetailDto(int Id, string Name, string? NameRu, string? NameTj,
                                 string? Description, int DoctorsCount);

public class SpecialtyRequest
{
    [Required, MaxLength(100)] public string Name { get; set; } = null!;
    [MaxLength(100)] public string? NameRu { get; set; }
    [MaxLength(100)] public string? NameTj { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }
}

// ---- Clinics ----
public record ClinicDto(int Id, string Name, int? CityId, string? CityName, string? Address,
                        string? Phone, string? Description, bool IsActive, int DoctorsCount);

public class ClinicRequest
{
    [Required, MaxLength(200)] public string Name { get; set; } = null!;
    public int? CityId { get; set; }
    [MaxLength(300)] public string? Address { get; set; }
    [MaxLength(20)] public string? Phone { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

// ---- Admin ----
public class SetActiveRequest
{
    public bool IsActive { get; set; }
}