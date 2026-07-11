using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PokeBinder.Api;

public static class CurrentUserExtensions
{
    public static string GetUserId(this ControllerBase controller) =>
        controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Request is authenticated but has no NameIdentifier claim.");
}
