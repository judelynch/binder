using PokeBinder.Core.Pricing;

namespace PokeBinder.Infrastructure.Pricing;

/// <summary>
/// Fixture-fed stand-in for a real price source, used in dev and integration/orchestrator tests.
/// Deterministic per query (seeded from CardVariantId) so repeated calls return the same
/// listings/ids - this exercises RawListing's dedupe-on-ListingId behavior the same way a real
/// re-scrape would, without actually needing to hit anything twice. The classifier's own
/// correctness is validated directly against IListingClassifier with real-shaped titles, not
/// through this provider - this only needs to produce plausible, clean, well-formed listings so
/// the orchestrator/scheduling/aggregation pipeline has something real to chew on end-to-end.
/// </summary>
public class MockPriceSourceProvider : IPriceSourceProvider
{
    public string ProviderName => "Mock";

    public Task<IReadOnlyList<RawListingDto>> GetListingsAsync(PriceQuery query, CancellationToken ct)
    {
        var rng = new Random(query.CardVariantId.GetHashCode());
        var count = rng.Next(3, 9);
        var basePrice = 2m + (decimal)rng.NextDouble() * 48m;

        var listings = new List<RawListingDto>(count);
        for (var i = 0; i < count; i++)
        {
            var variance = 0.8m + (decimal)rng.NextDouble() * 0.4m;
            var price = Math.Round(basePrice * variance, 2);
            var format = rng.NextDouble() < 0.7 ? ListingFormat.BuyItNow : ListingFormat.Auction;
            var postage = rng.NextDouble() < 0.5 ? 0m : Math.Round(1m + (decimal)rng.NextDouble() * 3m, 2);

            listings.Add(new RawListingDto(
                ListingId: $"{query.CardVariantId}-{i}",
                Title: $"{query.CardName} {query.SetNumber} {query.VariantTypeName}",
                ItemPriceGbp: price,
                PostagePriceGbp: postage,
                SoldDate: DateTime.UtcNow.AddDays(-rng.Next(1, 45)),
                ListingFormat: format,
                ThumbnailUrl: null));
        }

        return Task.FromResult<IReadOnlyList<RawListingDto>>(listings);
    }
}
