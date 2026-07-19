using System.Security.Claims;
using eCommerce.Server.Application.Common.Results;
using eCommerce.Server.Application.DTOs.Auth;
using eCommerce.Server.Application.Interfaces;
using eCommerce.Server.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace eCommerce.Server.Application.Services;

public class AuthAppService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ITokenService tokenService,
    IRefreshTokenStore refreshTokenStore) : IAuthService
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var user = new AppUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
            return Result<AuthResponse>.Fail(StatusCodes.Status400BadRequest, errors);
        }

        await userManager.AddToRoleAsync(user, "Customer");

        var roles = await userManager.GetRolesAsync(user);
        return Result<AuthResponse>.Ok(BuildAuthResponse(user, roles));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Result<AuthResponse>.Fail(StatusCodes.Status401Unauthorized, "Invalid credentials.");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Result<AuthResponse>.Fail(StatusCodes.Status429TooManyRequests, "Account locked out.");

            return Result<AuthResponse>.Fail(StatusCodes.Status401Unauthorized, "Invalid credentials.");
        }

        var roles = await userManager.GetRolesAsync(user);
        return Result<AuthResponse>.Ok(BuildAuthResponse(user, roles));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(request.Token);
        if (principal is null)
            return Result<AuthResponse>.Fail(StatusCodes.Status401Unauthorized, "Invalid token.");

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? principal.FindFirst("sub")?.Value;

        if (userId is null || !refreshTokenStore.TryGet(request.RefreshToken, out var storedUserId, out var expiry))
            return Result<AuthResponse>.Fail(StatusCodes.Status401Unauthorized, "Invalid refresh token.");

        if (storedUserId != userId || expiry < DateTime.UtcNow)
        {
            refreshTokenStore.Remove(request.RefreshToken);
            return Result<AuthResponse>.Fail(StatusCodes.Status401Unauthorized, "Refresh token expired.");
        }

        refreshTokenStore.Remove(request.RefreshToken);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result<AuthResponse>.Fail(StatusCodes.Status401Unauthorized, "Invalid user.");

        var roles = await userManager.GetRolesAsync(user);
        return Result<AuthResponse>.Ok(BuildAuthResponse(user, roles));
    }

    public Result Logout(string refreshToken)
    {
        refreshTokenStore.Remove(refreshToken);
        return Result.Ok(StatusCodes.Status204NoContent);
    }

    public async Task<Result<UserProfileDto>> GetMeAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result<UserProfileDto>.Fail(StatusCodes.Status404NotFound, "User not found.");

        var roles = await userManager.GetRolesAsync(user);

        return Result<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Roles = roles
        });
    }

    private AuthResponse BuildAuthResponse(AppUser user, IList<string> roles)
    {
        var token = tokenService.GenerateJwtToken(user, roles);
        var refreshToken = tokenService.GenerateRefreshToken();
        var expiry = DateTime.UtcNow.AddDays(7);

        refreshTokenStore.Set(refreshToken, user.Id, expiry);

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            Email = user.Email!,
            FullName = user.FullName,
            Roles = roles
        };
    }
}
