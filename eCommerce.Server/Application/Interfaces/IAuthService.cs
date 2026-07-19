using eCommerce.Server.Application.Common.Results;
using eCommerce.Server.Application.DTOs.Auth;

namespace eCommerce.Server.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request);
    Result Logout(string refreshToken);
    Task<Result<UserProfileDto>> GetMeAsync(string userId);
}
