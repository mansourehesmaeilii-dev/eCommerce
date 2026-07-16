using eCommerce.Server.Application.DTOs.Auth;
using eCommerce.Server.Domain.Entities;

namespace eCommerce.Server.Application.Interfaces;

public interface ITokenService
{
    string GenerateJwtToken(AppUser user, IList<string> roles);
    string GenerateRefreshToken();
    /// <summary>Validates an expired JWT and returns its principal claims.</summary>
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
