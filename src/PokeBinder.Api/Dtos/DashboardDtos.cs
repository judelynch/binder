namespace PokeBinder.Api.Dtos;

public record DashboardBinderDto(Guid Id, string Name, string ColourHex, double CompletenessPercent, DateTime? LastAccessedAt);

public record DashboardValuableCardDto(
    Guid CardVariantId,
    string CardId,
    string CardName,
    string? ImageSmallUrl,
    string SetName,
    string Number,
    string? VariantTypeName,
    decimal PriceGbp);

public record DashboardResponseDto(
    int CardsOwned,
    int CardsMissing,
    int BinderCount,
    IReadOnlyList<DashboardBinderDto> RecentBinders,
    decimal? PortfolioValueGbp,
    IReadOnlyList<DashboardValuableCardDto> TopValuableCards);
