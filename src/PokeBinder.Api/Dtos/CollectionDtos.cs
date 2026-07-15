namespace PokeBinder.Api.Dtos;

public record SetOwnershipRequest(int Quantity, string? Condition);

public record CardOwnershipDto(Guid CardVariantId, bool Owned, int Quantity, string? Condition);

public record BulkSetOwnershipRequest(IReadOnlyList<Guid> CardVariantIds, bool Owned);

public record BulkOwnershipResultDto(int Count);
