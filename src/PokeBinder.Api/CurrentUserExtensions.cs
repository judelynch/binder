using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PokeBinder.Api;

public static class CurrentUserExtensions
{
    public static string GetUserId(this ControllerBase controller) =>
        controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Request is authenticated but has no NameIdentifier claim.");

    public static string GetEmail(this ControllerBase controller) =>
        // ASP.NET Core's JWT handler remaps the short "email" claim type to the long ClaimTypes.Email
        // URI by default (MapInboundClaims defaults to true), so the long form is what's actually on
        // the authenticated principal — check it first, short form as a fallback for safety.
        controller.User.FindFirst(ClaimTypes.Email)?.Value
        ?? controller.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
        ?? string.Empty;
}
