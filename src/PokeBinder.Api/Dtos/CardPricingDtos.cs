namespace PokeBinder.Api.Dtos;

/// <summary>One individual accepted eBay sale, with everything we captured about it - backs both
/// the card detail page's trend chart and its full sale-history list.</summary>
public record PriceHistoryPointDto(
    DateTime SoldDate,
    string Title,
    decimal ItemPriceGbp,
    decimal? PostagePriceGbp,
    decimal DeliveredPriceGbp,
    string ListingFormat,
    string? ThumbnailUrl,
    string GradedStatus,
    string? Grader,
    decimal? Grade,
    string RawCondition);
