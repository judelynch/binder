using System.ComponentModel.DataAnnotations;

namespace PokeBinder.Api.Dtos;

public record CreateBinderRequest(
    [Required, MaxLength(200)] string Name,
    [Required, RegularExpression("^#[0-9A-Fa-f]{6}$")] string ColourHex,
    [Range(1, 20)] int Rows,
    [Range(1, 20)] int Columns,
    [Range(0, 500)] int InitialPageCount);

public record UpdateBinderRequest(
    [MaxLength(200)] string? Name,
    [RegularExpression("^#[0-9A-Fa-f]{6}$")] string? ColourHex);

public record AppendPagesRequest([Range(2, 500)] int Count);

public record BinderSummaryDto(
    Guid Id,
    string Name,
    string ColourHex,
    int Rows,
    int Columns,
    int PageCount,
    int TotalSlots,
    int FilledSlots,
    int OwnedCount,
    int MissingCount,
    DateTime CreatedAt,
    DateTime? LastAccessedAt);
