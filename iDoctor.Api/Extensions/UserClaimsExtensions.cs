using System.Security.Claims;

namespace iDoctor.Api.Extensions;

public static class UserClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException();
    }
}