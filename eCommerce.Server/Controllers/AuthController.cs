using System.Security.Claims;
using eCommerce.Server.Application.Common.Results;
using eCommerce.Server.Application.DTOs.Auth;
using eCommerce.Server.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService) : ControllerBase
{
    // ------------------------------------------------------------------ //
    // POST api/auth/register
    // ------------------------------------------------------------------ //
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await authService.RegisterAsync(request);
        return ToActionResult(result);
    }

    // ------------------------------------------------------------------ //
    // POST api/auth/login
    // ------------------------------------------------------------------ //
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await authService.LoginAsync(request);
        return ToActionResult(result);
    }

    // ------------------------------------------------------------------ //
    // POST api/auth/refresh
    // ------------------------------------------------------------------ //
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await authService.RefreshAsync(request);
        return ToActionResult(result);
    }

    // ------------------------------------------------------------------ //
    // POST api/auth/logout
    // ------------------------------------------------------------------ //
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout([FromBody] string refreshToken)
    {
        var result = authService.Logout(refreshToken);
        return ToActionResult(result);
    }

    // ------------------------------------------------------------------ //
    // GET api/auth/me
    // ------------------------------------------------------------------ //
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await authService.GetMeAsync(userId);
        return ToActionResult(result);
    }

    // ------------------------------------------------------------------ //
    // Helpers
    // ------------------------------------------------------------------ //
    private IActionResult ToActionResult(Result result) =>
        result.Success
            ? result.StatusCode switch
            {
                StatusCodes.Status204NoContent => NoContent(),
                _ => Ok()
            }
            : result.StatusCode switch
            {
                StatusCodes.Status401Unauthorized => Unauthorized(result.Error),
                StatusCodes.Status400BadRequest => BadRequest(result.Error),
                StatusCodes.Status404NotFound => NotFound(result.Error),
                _ => StatusCode(result.StatusCode, result.Error)
            };

    private IActionResult ToActionResult<T>(Result<T> result) =>
        result.Success
            ? Ok(result.Data)
            : result.StatusCode switch
            {
                StatusCodes.Status401Unauthorized => Unauthorized(result.Error),
                StatusCodes.Status400BadRequest => BadRequest(result.Error),
                StatusCodes.Status404NotFound => NotFound(result.Error),
                _ => StatusCode(result.StatusCode, result.Error)
            };
}
