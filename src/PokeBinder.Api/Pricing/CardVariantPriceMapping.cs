using PokeBinder.Api.Dtos;
using PokeBinder.Core.Pricing;

namespace PokeBinder.Api.Pricing;

/// <summary>Shared PricePoint -> DTO mapping, used by both the binder-scoped and card-scoped price endpoints.</summary>
public static class CardVariantPriceMapping
{
    public static CardVariantPriceDto ToDto(Guid cardVariantId, List<PricePoint> points, DateTime? lastScrapedAt)
    {
        PriceBucketDto ToBucket(PricePoint p) => new(
            p.GradedStatus.ToString(), p.Grader, p.Grade, p.Condition?.ToString(),
            p.WindowDays, p.ItemOnlyMedianGbp, p.DeliveredMedianGbp, p.SampleCount, p.LastSaleDate);

        var rawPoints = points.Where(p => p.GradedStatus == GradedStatus.Raw).ToList();
        var rawBuckets = rawPoints.OrderBy(p => p.Condition).ThenBy(p => p.WindowDays).Select(ToBucket).ToList();
        var gradedBuckets = points.Where(p => p.GradedStatus == GradedStatus.Graded)
            .OrderBy(p => p.Grader).ThenByDescending(p => p.Grade).ThenBy(p => p.WindowDays)
            .Select(ToBucket).ToList();

        var cheapestRaw = rawPoints.OrderBy(p => p.ItemOnlyMedianGbp).FirstOrDefault();

        return new CardVariantPriceDto(
            cardVariantId,
            cheapestRaw?.ItemOnlyMedianGbp,
            cheapestRaw?.DeliveredMedianGbp,
            rawBuckets,
            gradedBuckets,
            lastScrapedAt);
    }
}
