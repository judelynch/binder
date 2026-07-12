namespace PokeBinder.Api.Dtos;

public record SetSummaryDto(
    string Id,
    string Name,
    string Series,
    int PrintedTotal,
    int Total,
    DateOnly ReleaseDate,
    string? PtcgoCode,
    string? SymbolImageUrl,
    string? LogoImageUrl);

public record VariantSummaryDto(Guid Id, string VariantTypeName);

public record CardSummaryDto(
    string Id,
    string SetId,
    string Name,
    string Number,
    string? Rarity,
    string Supertype,
    string? ImageSmallUrl,
    string? ImageLargeUrl,
    IReadOnlyList<VariantSummaryDto> Variants);

public record CardDetailDto(
    string Id,
    string SetId,
    string Name,
    string Supertype,
    IReadOnlyList<string> Subtypes,
    string? Level,
    string? Hp,
    IReadOnlyList<string> Types,
    string? EvolvesFrom,
    string Number,
    string? Artist,
    string? Rarity,
    string? FlavorText,
    string? RegulationMark,
    IReadOnlyList<int> NationalPokedexNumbers,
    string? ImageSmallUrl,
    string? ImageLargeUrl,
    IReadOnlyList<string> VariantTypeNames);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
