using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TarjetasPro.Application.Auth;

namespace TarjetasPro.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequest request) =>
        ToMessage(await auth.RegisterAsync(request), 400);

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request) =>
        ToMessage(await auth.ConfirmEmailAsync(request), 400);

    [HttpPost("resend-confirmation")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest request) =>
        ToMessage(await auth.ResendConfirmationAsync(request.Email), 400);

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request) =>
        ToResponse(await auth.LoginAsync(request), 401);

    /// <summary>Qué proveedores externos están configurados (ids públicos para el botón del navegador).</summary>
    [HttpGet("providers")]
    public IActionResult Providers() => Ok(auth.GetProviders());

    [HttpPost("external")]
    public async Task<IActionResult> External(ExternalLoginRequest request) =>
        ToResponse(await auth.ExternalLoginAsync(request), 401);

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request) =>
        ToResponse(await auth.RefreshAsync(request.RefreshToken), 401);

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        await auth.LogoutAsync(request.RefreshToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var user = Guid.TryParse(id, out var guid) ? await auth.GetUserAsync(guid) : null;
        return user is null ? Unauthorized() : Ok(user);
    }

    private IActionResult ToMessage(MessageResult result, int failStatus) =>
        result.Ok ? Ok(new { message = result.Message }) : StatusCode(failStatus, new { errors = result.Errors });

    private IActionResult ToResponse(AuthResult result, int failStatus) =>
        result.Ok ? Ok(result.Data) : StatusCode(failStatus, new { errors = result.Errors });
}
