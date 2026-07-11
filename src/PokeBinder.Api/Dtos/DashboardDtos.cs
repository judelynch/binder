namespace PokeBinder.Api.Dtos;

public record DashboardBinderDto(Guid Id, string Name, string ColourHex, double CompletenessPercent, DateTime? LastAccessedAt);

public record DashboardResponseDto(int CardsOwned, int CardsMissing, int BinderCount, IReadOnlyList<DashboardBinderDto> RecentBinders);
