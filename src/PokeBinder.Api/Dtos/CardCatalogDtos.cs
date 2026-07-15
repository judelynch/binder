using PokeBinder.Core.Cards;

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
    string? LogoImageUrl,
    int CardCount,
    int OwnedCount);

public record VariantSummaryDto(Guid Id, string VariantTypeName);

/// <summary>Like VariantSummaryDto, plus the current user's ownership of this specific variant.</summary>
public record OwnedVariantSummaryDto(Guid Id, string VariantTypeName, bool Owned, int Quantity, string? Condition);

public record CardSummaryDto(
    string Id,
    string SetId,
    string Name,
    string Number,
    string? Rarity,
    string Supertype,
    string? ImageSmallUrl,
    string? ImageLargeUrl,
    IReadOnlyList<OwnedVariantSummaryDto> Variants);

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
    IReadOnlyList<Ability> Abilities,
    IReadOnlyList<Attack> Attacks,
    IReadOnlyList<TypeEffect> Weaknesses,
    IReadOnlyList<TypeEffect> Resistances,
    IReadOnlyList<string> RetreatCost,
    int? ConvertedRetreatCost,
    string Number,
    string? Artist,
    string? Rarity,
    string? FlavorText,
    string? RegulationMark,
    IReadOnlyList<int> NationalPokedexNumbers,
    string? ImageSmallUrl,
    string? ImageLargeUrl,
    IReadOnlyList<OwnedVariantSummaryDto> Variants);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public record CardSearchResultDto(
    string Id,
    string SetId,
    string SetName,
    string Name,
    string Number,
    string? Rarity,
    string Supertype,
    string? ImageSmallUrl,
    string? ImageLargeUrl,
    IReadOnlyList<VariantSummaryDto> Variants);
