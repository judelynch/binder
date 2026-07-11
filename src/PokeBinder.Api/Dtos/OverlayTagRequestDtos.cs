using System.ComponentModel.DataAnnotations;

namespace PokeBinder.Api.Dtos;

public record CreateOverlayTagRequest(
    [Required, MaxLength(100)] string Name,
    [Required, RegularExpression("^#[0-9A-Fa-f]{6}$")] string ColourHex);

public record UpdateOverlayTagRequest(
    [MaxLength(100)] string? Name,
    [RegularExpression("^#[0-9A-Fa-f]{6}$")] string? ColourHex);
