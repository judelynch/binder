namespace PokeBinder.Core.Pricing;

/// <summary>Everything needed to build a source-specific search query for one card variant.</summary>
public record PriceQuery(Guid CardVariantId, string CardName, string SetNumber, string VariantTypeName);

/// <summary>
/// An already-structured listing as returned by a provider - acquisition and parsing are
/// entirely the provider's own concern. The pipeline (IListingClassifier onward) never sees raw
/// HTML or knows how a listing was fetched.
/// </summary>
public record RawListingDto(
    string ListingId,
    string Title,
    decimal ItemPriceGbp,
    decimal? PostagePriceGbp,
    DateTime SoldDate,
    ListingFormat ListingFormat,
    string? ThumbnailUrl);

/// <summary>
/// Source of sold-listing data for the pricing pipeline. A real implementation (eBay Partner
/// Network, TCGplayer, etc.) does its own fetch+parse internally and returns the same
/// RawListingDto shape - nothing downstream of this interface changes when the source does.
/// </summary>
public interface IPriceSourceProvider
{
    string ProviderName { get; }
    Task<IReadOnlyList<RawListingDto>> GetListingsAsync(PriceQuery query, CancellationToken ct);
}
