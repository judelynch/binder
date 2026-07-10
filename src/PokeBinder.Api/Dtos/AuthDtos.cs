using System.ComponentModel.DataAnnotations;

namespace PokeBinder.Api.Dtos;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record AuthResponse(string Token, string UserId, string Email, IReadOnlyList<string> Roles);

public record MeResponse(string UserId, string Email, IReadOnlyList<string> Roles);
