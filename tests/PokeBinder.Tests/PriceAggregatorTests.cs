using PokeBinder.Core.Pricing;
using Xunit;

namespace PokeBinder.Tests;

public class PriceAggregatorTests
{
    private readonly PriceAggregator _aggregator = new();
    private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    private static AggregationListing Raw(
        decimal price, decimal? postage = 0m, int daysAgo = 5, RawConditionClassification condition = RawConditionClassification.NM,
        bool bestOffer = false, string language = "English", ClassificationStatus status = ClassificationStatus.AutoAccepted) =>
        new(GradedStatus.Raw, null, null, condition, bestOffer, language, status, price, postage, Now.AddDays(-daysAgo));

    private static AggregationListing Graded(
        decimal price, string grader = "PSA", decimal grade = 10m, decimal? postage = 0m, int daysAgo = 5,
        ClassificationStatus status = ClassificationStatus.AutoAccepted) =>
        new(GradedStatus.Graded, grader, grade, RawConditionClassification.Unspecified, false, "English", status, price, postage, Now.AddDays(-daysAgo));

    [Fact]
    public void MedianOfOddSampleCount_IsMiddleValue()
    {
        var listings = new[] { Raw(10m), Raw(20m), Raw(30m) };
        var result = _aggregator.Aggregate(listings, Now);
        var point = Assert.Single(result.Where(p => p.WindowDays == 30));
        Assert.Equal(20m, point.ItemOnlyMedianGbp);
    }

    [Fact]
    public void MedianOfEvenSampleCount_IsAverageOfMiddleTwo()
    {
        var listings = new[] { Raw(10m), Raw(20m), Raw(30m), Raw(40m) };
        var result = _aggregator.Aggregate(listings, Now);
        var point = Assert.Single(result.Where(p => p.WindowDays == 30));
        Assert.Equal(25m, point.ItemOnlyMedianGbp);
    }

    [Fact]
    public void FewerThanThreeSales_ProducesNoPricePoint()
    {
        var listings = new[] { Raw(10m), Raw(20m) };
        var result = _aggregator.Aggregate(listings, Now);
        Assert.Empty(result);
    }

    [Fact]
    public void ExactlyThreeSales_ProducesAPricePoint()
    {
        var listings = new[] { Raw(10m), Raw(20m), Raw(30m) };
        var result = _aggregator.Aggregate(listings, Now);
        Assert.NotEmpty(result.Where(p => p.WindowDays == 30));
    }

    [Fact]
    public void OutlierBeyond1_5Iqr_IsDiscardedFromMedian()
    {
        // Tight cluster around 10 plus one wild outlier at 1000. Needs enough non-outlier points on
        // both sides of the split that the outlier doesn't dominate its own quartile's median (with
        // too few points, the outlier drags Q3 up enough to make the IQR bound too wide to exclude
        // itself) - 7 points total, 6 of them tightly clustered, isolates the outlier cleanly.
        var listings = new[] { Raw(9m), Raw(10m), Raw(10m), Raw(10m), Raw(11m), Raw(11m), Raw(1000m) };
        var result = _aggregator.Aggregate(listings, Now);
        var point = Assert.Single(result.Where(p => p.WindowDays == 30));
        Assert.Equal(6, point.SampleCount); // outlier excluded from the kept set
        Assert.True(point.MaxGbp < 100m, $"expected outlier excluded, got max {point.MaxGbp}");
    }

    [Fact]
    public void ItemOnlyAndDeliveredMedians_DifferByPostage()
    {
        var listings = new[] { Raw(10m, postage: 2m), Raw(20m, postage: 2m), Raw(30m, postage: 2m) };
        var result = _aggregator.Aggregate(listings, Now);
        var point = Assert.Single(result.Where(p => p.WindowDays == 30));
        Assert.Equal(20m, point.ItemOnlyMedianGbp);
        Assert.Equal(22m, point.DeliveredMedianGbp);
    }

    [Fact]
    public void NullPostage_TreatedAsZeroForDeliveredMedian()
    {
        var listings = new[] { Raw(10m, postage: null), Raw(20m, postage: null), Raw(30m, postage: null) };
        var result = _aggregator.Aggregate(listings, Now);
        var point = Assert.Single(result.Where(p => p.WindowDays == 30));
        Assert.Equal(point.ItemOnlyMedianGbp, point.DeliveredMedianGbp);
    }

    [Fact]
    public void BestOfferAccepted_ExcludedFromAggregation()
    {
        var listings = new[] { Raw(10m), Raw(20m), Raw(30m), Raw(9999m, bestOffer: true) };
        var result = _aggregator.Aggregate(listings, Now);
        var point = Assert.Single(result.Where(p => p.WindowDays == 30));
        Assert.Equal(3, point.SampleCount);
        Assert.True(point.MaxGbp < 100m);
    }

    [Fact]
    public void NonEnglishListings_ExcludedFromAggregation()
    {
        var listings = new[] { Raw(10m), Raw(20m), Raw(30m), Raw(9999m, language: "Japanese") };
        var result = _aggregator.Aggregate(listings, Now);
        var point = Assert.Single(result.Where(p => p.WindowDays == 30));
        Assert.Equal(3, point.SampleCount);
    }

    [Fact]
    public void QuarantinedOrRejectedListings_NeverAggregated()
    {
        var listings = new[]
        {
            Raw(10m), Raw(20m), Raw(30m),
            Raw(9999m, status: ClassificationStatus.Quarantined),
            Raw(9999m, status: ClassificationStatus.Rejected),
        };
        var result = _aggregator.Aggregate(listings, Now);
        var point = Assert.Single(result.Where(p => p.WindowDays == 30));
        Assert.Equal(3, point.SampleCount);
    }

    [Fact]
    public void RawBuckets_GroupedByConditionSeparately()
    {
        var listings = new[]
        {
            Raw(10m, condition: RawConditionClassification.NM), Raw(11m, condition: RawConditionClassification.NM), Raw(12m, condition: RawConditionClassification.NM),
            Raw(5m, condition: RawConditionClassification.LP), Raw(6m, condition: RawConditionClassification.LP), Raw(7m, condition: RawConditionClassification.LP),
        };
        var result = _aggregator.Aggregate(listings, Now).Where(p => p.WindowDays == 30).ToList();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Condition == RawConditionClassification.NM && p.ItemOnlyMedianGbp == 11m);
        Assert.Contains(result, p => p.Condition == RawConditionClassification.LP && p.ItemOnlyMedianGbp == 6m);
    }

    [Fact]
    public void UnspecifiedCondition_IsItsOwnBucket()
    {
        var listings = new[] { Raw(10m, condition: RawConditionClassification.Unspecified), Raw(11m, condition: RawConditionClassification.Unspecified), Raw(12m, condition: RawConditionClassification.Unspecified) };
        var result = _aggregator.Aggregate(listings, Now);
        var point = Assert.Single(result.Where(p => p.WindowDays == 30));
        Assert.Equal(RawConditionClassification.Unspecified, point.Condition);
    }

    [Fact]
    public void GradedBuckets_GroupedByGraderAndGradeSeparately()
    {
        var listings = new[]
        {
            Graded(100m, "PSA", 10m), Graded(110m, "PSA", 10m), Graded(105m, "PSA", 10m),
            Graded(40m, "PSA", 9m), Graded(45m, "PSA", 9m), Graded(42m, "PSA", 9m),
        };
        var result = _aggregator.Aggregate(listings, Now).Where(p => p.WindowDays == 30).ToList();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Grade == 10m && p.Condition == null);
        Assert.Contains(result, p => p.Grade == 9m && p.Condition == null);
    }

    [Fact]
    public void ListingOutsideAllWindows_Excluded()
    {
        var listings = new[] { Raw(10m, daysAgo: 200), Raw(20m, daysAgo: 200), Raw(30m, daysAgo: 200) };
        var result = _aggregator.Aggregate(listings, Now);
        Assert.Empty(result);
    }

    [Fact]
    public void SameListings_ProduceIndependentPricePointsPerWindow()
    {
        var listings = new[] { Raw(10m, daysAgo: 5), Raw(20m, daysAgo: 5), Raw(30m, daysAgo: 5) };
        var result = _aggregator.Aggregate(listings, Now);
        Assert.Equal(3, result.Count); // one per 30/60/90-day window, all containing the same 3 sales
        Assert.All(result, p => Assert.Equal(3, p.SampleCount));
    }

    [Fact]
    public void CardmarketSanityCheck_StubbedAlwaysPasses_ThisPhase()
    {
        var listings = new[] { Raw(10m), Raw(20m), Raw(30m) };
        var result = _aggregator.Aggregate(listings, Now);
        Assert.All(result, p => Assert.Null(p.QuarantinedReason));
    }
}
