namespace PokeBinder.Core.Pricing;

public enum ListingFormat
{
    Auction,
    BuyItNow,
    BestOfferAccepted,
}

/// <summary>
/// A single structured listing as returned by an IPriceSourceProvider, persisted so
/// re-classification can replay history without re-fetching. CardVariantId is the variant that
/// was QUERIED for - the listing's actual resolved variant (which may differ, see
/// ListingClassification.ResolvedCardVariantId) is a classification-time decision, not a raw-data fact.
/// </summary>
public class RawListing
{
    public Guid Id { get; set; }
    public Guid CardVariantId { get; set; }
    public string Query { get; set; } = string.Empty;

    /// <summary>The source provider's own listing id - used for cross-run dedupe, not this table's PK.</summary>
    public string ListingId { get; set; } = string.Empty;
    public string SourceProvider { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public decimal ItemPriceGbp { get; set; }

    /// <summary>Null when unparseable, 0 when the listing states free postage.</summary>
    public decimal? PostagePriceGbp { get; set; }
    public DateTime SoldDate { get; set; }
    public ListingFormat ListingFormat { get; set; }
    public string? ThumbnailUrl { get; set; }

    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}
