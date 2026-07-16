using System.Collections.Concurrent;
using eCommerce.Server.Application.DTOs.Auth;
using eCommerce.Server.Application.Interfaces;
using eCommerce.Server.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ITokenService tokenService) : ControllerBase
{
    // Temporary in-memory refresh token store — replace with DB in Phase 2
    private static readonly ConcurrentDictionary<string, (string UserId, DateTime Expiry)> _refreshTokens = new();

    // ------------------------------------------------------------------ //
    // POST api/auth/register
    // ------------------------------------------------------------------ //
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new AppUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        await userManager.AddToRoleAsync(user, "Customer");

        var roles = await userManager.GetRolesAsync(user);
        return Ok(BuildAuthResponse(user, roles));
    }

    // ------------------------------------------------------------------ //
    // POST api/auth/login
    // ------------------------------------------------------------------ //
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized("Invalid credentials.");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut) return StatusCode(429, "Account locked out.");
            return Unauthorized("Invalid credentials.");
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(BuildAuthResponse(user, roles));
    }

    // ------------------------------------------------------------------ //
    // POST api/auth/refresh
    // ------------------------------------------------------------------ //
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(request.Token);
        if (principal is null)
            return Unauthorized("Invalid token.");

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst("sub")?.Value;

        if (userId is null || !_refreshTokens.TryGetValue(request.RefreshToken, out var stored))
            return Unauthorized("Invalid refresh token.");

        if (stored.UserId != userId || stored.Expiry < DateTime.UtcNow)
        {
            _refreshTokens.TryRemove(request.RefreshToken, out _);
            return Unauthorized("Refresh token expired.");
        }

        _refreshTokens.TryRemove(request.RefreshToken, out _);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        return Ok(BuildAuthResponse(user, roles));
    }

    // ------------------------------------------------------------------ //
    // POST api/auth/logout
    // ------------------------------------------------------------------ //
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout([FromBody] string refreshToken)
    {
        _refreshTokens.TryRemove(refreshToken, out _);
        return NoContent();
    }

    // ------------------------------------------------------------------ //
    // GET api/auth/me
    // ------------------------------------------------------------------ //
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        var user = await userManager.FindByIdAsync(userId!);
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.FullName,
            Roles = roles
        });
    }

    // ------------------------------------------------------------------ //
    // Helpers
    // ------------------------------------------------------------------ //
    private AuthResponse BuildAuthResponse(AppUser user, IList<string> roles)
    {
        var token = tokenService.GenerateJwtToken(user, roles);
        var refreshToken = tokenService.GenerateRefreshToken();
        var expiry = DateTime.UtcNow.AddDays(7);

        _refreshTokens[refreshToken] = (user.Id, expiry);

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
