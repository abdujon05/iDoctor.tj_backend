using Microsoft.AspNetCore.Identity;

namespace iDoctor.Domain.Entities;

// Id, Email, PhoneNumber, PasswordHash, UserName etc. come from IdentityUser<Guid>
public class User : IdentityUser<Guid>
{
    public string FullName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
}