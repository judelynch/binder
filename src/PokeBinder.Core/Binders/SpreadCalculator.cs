namespace PokeBinder.Core.Binders;

public enum SpreadPanelType
{
    Cover,
    Page
}

public readonly record struct SpreadPanel(SpreadPanelType Type, int? PageNumber);

public readonly record struct SpreadResult(SpreadPanel Left, SpreadPanel Right, int TotalSpreads);

/// <summary>
/// Binder page counts are always even (pages exist in physical sheet-pairs — see
/// BinderPage). That invariant collapses the spread math to one formula with no
/// odd/even branching: spread 0 is always front-cover + page 1 (or front-cover +
/// back-cover for an empty 0-page binder). Every subsequent spread k pairs page
/// 2k with page 2k+1, except the very last spread, where 2k+1 always exceeds the
/// (even) page count, so the right panel is always the back cover there.
/// </summary>
public static class SpreadCalculator
{
    public static int GetTotalSpreads(int pageCount)
    {
        if (pageCount < 0 || pageCount % 2 != 0)
        {
            throw new ArgumentException("Page count must be even and non-negative.", nameof(pageCount));
        }

        return pageCount == 0 ? 1 : 1 + pageCount / 2;
    }

    public static SpreadResult GetSpread(int pageCount, int spreadIndex)
    {
        var totalSpreads = GetTotalSpreads(pageCount);
        if (spreadIndex < 0 || spreadIndex >= totalSpreads)
        {
            throw new ArgumentOutOfRangeException(nameof(spreadIndex), $"spreadIndex must be in [0, {totalSpreads - 1}].");
        }

        if (spreadIndex == 0)
        {
            var right = pageCount >= 1 ? Page(1) : Cover();
            return new SpreadResult(Cover(), right, totalSpreads);
        }

        var leftPageNumber = spreadIndex * 2;
        var rightPageNumber = leftPageNumber + 1;
        var rightPanel = rightPageNumber <= pageCount ? Page(rightPageNumber) : Cover();
        return new SpreadResult(Page(leftPageNumber), rightPanel, totalSpreads);
    }

    private static SpreadPanel Cover() => new(SpreadPanelType.Cover, null);
    private static SpreadPanel Page(int pageNumber) => new(SpreadPanelType.Page, pageNumber);

    /// <summary>
    /// The pair of page numbers making up the physical sheet (leaf) that the given
    /// page number belongs to. Leaf k = pages (2k-1, 2k), so deleting either page
    /// of a sheet removes both — you can't remove one face without the other.
    /// </summary>
    public static (int First, int Second) GetSheetPair(int pageNumber)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        var leafIndex = (pageNumber + 1) / 2;
        return (leafIndex * 2 - 1, leafIndex * 2);
    }
}
