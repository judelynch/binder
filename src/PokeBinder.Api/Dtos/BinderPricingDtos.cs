namespace PokeBinder.Api.Dtos;

public record PriceBucketDto(
    string GradedStatus,
    string? Grader,
    decimal? Grade,
    string? Condition,
    int WindowDays,
    decimal ItemOnlyMedianGbp,
    decimal DeliveredMedianGbp,
    int SampleCount,
    DateTime LastSaleDate);

public record CardVariantPriceDto(
    Guid CardVariantId,
    decimal? BestAvailableItemOnlyGbp,
    decimal? BestAvailableDeliveredGbp,
    IReadOnlyList<PriceBucketDto> RawBuckets,
    IReadOnlyList<PriceBucketDto> GradedBuckets,
    DateTime? LastScrapedAt);

public record BinderPriceSummaryDto(
    decimal? OwnedValueGbp,
    decimal? MissingCostGbp,
    IReadOnlyList<CardVariantPriceDto> Prices);
