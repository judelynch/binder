using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PokeBinder.Api.Dtos;
using PokeBinder.Core.Identity;
using PokeBinder.Core.Pricing;
using PokeBinder.Infrastructure.Pricing;

namespace PokeBinder.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly PricingScrapeOrchestrator _scrapeOrchestrator;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        PricingScrapeOrchestrator scrapeOrchestrator,
        IBackgroundJobClient backgroundJobClient)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _scrapeOrchestrator = scrapeOrchestrator;
        _backgroundJobClient = backgroundJobClient;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return ValidationProblem(BuildModelState(result));
        }

        await _userManager.AddToRoleAsync(user, Roles.User);

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.CreateAccessToken(user, roles);

        return Ok(new AuthResponse(token, user.Id, user.Email!, roles.ToList()));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.CreateAccessToken(user, roles);

        // Fire-and-forget catch-up: never blocks or fails the login response. Hangfire's own
        // Enqueue resolves a fresh DI scope when the job actually runs, so - unlike this app's
        // older hand-rolled Task.Run+IServiceScopeFactory pattern (see AdminController.ApplySync) -
        // no manual scope juggling is needed here.
        if (roles.Contains(Roles.Admin) && await _scrapeOrchestrator.ShouldRunCatchUpAsync(ct))
        {
            var userId = user.Id;
            _backgroundJobClient.Enqueue<PricingScrapeOrchestrator>(o => o.RunAsync(ScrapeTrigger.LoginCatchUp, userId, null, CancellationToken.None));
        }

        return Ok(new AuthResponse(token, user.Id, user.Email!, roles.ToList()));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new MeResponse(user.Id, user.Email!, roles.ToList()));
    }

    private static Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary BuildModelState(IdentityResult result)
    {
        var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        foreach (var error in result.Errors)
        {
            modelState.AddModelError(error.Code, error.Description);
        }
        return modelState;
    }
}
