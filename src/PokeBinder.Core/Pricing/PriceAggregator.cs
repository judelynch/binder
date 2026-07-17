namespace PokeBinder.Core.Pricing;

public class PriceAggregator : IPriceAggregator
{
    private static readonly int[] WindowsDays = { 30, 60, 90 };
    private const int MinimumSampleCount = 3;

    public IReadOnlyList<PricePointCandidate> Aggregate(IReadOnlyList<AggregationListing> listings, DateTime now)
    {
        // Only ever aggregate accepted listings - "accepted" covers both auto-accepted and
        // manually-approved-from-quarantine, since approving in the review queue flips a
        // listing's Status to AutoAccepted so it flows into the next run without special-casing.
        var eligible = listings
            .Where(l => l.Status == ClassificationStatus.AutoAccepted)
            .Where(l => !l.BestOfferAccepted)
            .Where(l => l.Language == "English")
            .ToList();

        var results = new List<PricePointCandidate>();

        foreach (var window in WindowsDays)
        {
            var cutoff = now.AddDays(-window);
            var inWindow = eligible.Where(l => l.SoldDate >= cutoff).ToList();

            var rawBuckets = inWindow
                .Where(l => l.GradedStatus == GradedStatus.Raw)
                .GroupBy(l => l.RawCondition);
            foreach (var bucket in rawBuckets)
            {
                var candidate = BuildCandidate(bucket.ToList(), window, GradedStatus.Raw, grader: null, grade: null, condition: bucket.Key);
                if (candidate is not null)
                {
                    results.Add(candidate);
                }
            }

            var gradedBuckets = inWindow
                .Where(l => l.GradedStatus == GradedStatus.Graded)
                .GroupBy(l => (l.Grader, l.Grade));
            foreach (var bucket in gradedBuckets)
            {
                var candidate = BuildCandidate(bucket.ToList(), window, GradedStatus.Graded, bucket.Key.Grader, bucket.Key.Grade, condition: null);
                if (candidate is not null)
                {
                    results.Add(candidate);
                }
            }
        }

        return results;
    }

    private static PricePointCandidate? BuildCandidate(
        List<AggregationListing> bucketListings, int windowDays, GradedStatus gradedStatus, string? grader, decimal? grade, RawConditionClassification? condition)
    {
        if (bucketListings.Count < MinimumSampleCount)
        {
            return null; // insufficient data - no row at all, not a quarantined one
        }

        var itemPrices = bucketListings.Select(l => l.ItemPriceGbp).OrderBy(p => p).ToList();
        var kept = DiscardOutliers(bucketListings, itemPrices);
        if (kept.Count < MinimumSampleCount)
        {
            return null; // outlier removal dropped us below the minimum too
        }

        var keptItemPrices = kept.Select(l => l.ItemPriceGbp).OrderBy(p => p).ToList();
        // Postage null ("unparseable") is treated the same as 0 ("free postage") for the delivered
        // total - both mean "no known additional cost to add", and real eBay listings essentially
        // never have genuinely unparseable postage in practice.
        var deliveredPrices = kept.Select(l => l.ItemPriceGbp + (l.PostagePriceGbp ?? 0m)).OrderBy(p => p).ToList();

        return new PricePointCandidate(
            GradedStatus: gradedStatus,
            Grader: grader,
            Grade: grade,
            Condition: condition,
            WindowDays: windowDays,
            ItemOnlyMedianGbp: Median(keptItemPrices),
            DeliveredMedianGbp: Median(deliveredPrices),
            SampleCount: kept.Count,
            MinGbp: keptItemPrices[0],
            MaxGbp: keptItemPrices[^1],
            LastSaleDate: kept.Max(l => l.SoldDate),
            // Cardmarket cross-check is intentionally stubbed always-pass this phase - the
            // pokemon-tcg-data source this app imports from doesn't carry cardmarket pricing at
            // all (confirmed against the live files, not just an unmapped field). Real ingestion
            // is a separate follow-up phase; this hook is where it plugs back in.
            QuarantinedReason: null);
    }

    /// <summary>Standard exclusive-median Tukey hinges: excludes the overall median itself from both halves when the count is odd, before computing Q1/Q3.</summary>
    private static List<AggregationListing> DiscardOutliers(List<AggregationListing> listings, List<decimal> sortedPrices)
    {
        var n = sortedPrices.Count;
        var halfSize = n / 2;
        var lowerHalf = sortedPrices.Take(halfSize).ToList();
        var upperHalf = sortedPrices.Skip(n - halfSize).ToList();

        var q1 = Median(lowerHalf);
        var q3 = Median(upperHalf);
        var iqr = q3 - q1;
        var lowerBound = q1 - 1.5m * iqr;
        var upperBound = q3 + 1.5m * iqr;

        return listings.Where(l => l.ItemPriceGbp >= lowerBound && l.ItemPriceGbp <= upperBound).ToList();
    }

    private static decimal Median(List<decimal> sortedValues)
    {
        var n = sortedValues.Count;
        if (n == 0)
        {
            return 0m;
        }

        return n % 2 == 1
            ? sortedValues[n / 2]
            : (sortedValues[n / 2 - 1] + sortedValues[n / 2]) / 2m;
    }
}
