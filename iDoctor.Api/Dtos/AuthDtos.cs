using System.ComponentModel.DataAnnotations;

namespace iDoctor.Api.Dtos;

public class RegisterRequestBase
{
    [Required, MaxLength(200)] public string FullName { get; set; } = null!;
    [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = null!;
    [Required, RegularExpression(@"^\+992\d{9}$", ErrorMessage = "Phone must look like +992XXXXXXXXX.")]
    public string PhoneNumber { get; set; } = null!;
    [Required, MinLength(8), MaxLength(100)] public string Password { get; set; } = null!;
}

public class RegisterPatientRequest : RegisterRequestBase
{
    public DateOnly? DateOfBirth { get; set; }
    [RegularExpression("^(Male|Female)$")] public string? Gender { get; set; }
    public int? CityId { get; set; }
}

public class RegisterDoctorRequest : RegisterRequestBase
{
    [MaxLength(4000)] public string? Bio { get; set; }
    [Range(0, 70)] public short ExperienceYears { get; set; }
    [Range(0, 100000)] public decimal? ConsultationPrice { get; set; }
    [Required, MinLength(1)] public List<int> SpecialtyIds { get; set; } = new();
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = null!;
    [Required] public string Password { get; set; } = null!;
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public IList<string> Roles { get; set; } = new List<string>();
}

public class AuthResponse
{
    public string AccessToken { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public UserInfo User { get; set; } = null!;
}