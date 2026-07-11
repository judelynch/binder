using PokeBinder.Core.Binders;
using Xunit;

namespace PokeBinder.Tests;

public class SpreadCalculatorTests
{
    [Fact]
    public void GetTotalSpreads_RejectsOddPageCounts()
    {
        Assert.Throws<ArgumentException>(() => SpreadCalculator.GetTotalSpreads(3));
    }

    [Fact]
    public void GetTotalSpreads_RejectsNegativePageCounts()
    {
        Assert.Throws<ArgumentException>(() => SpreadCalculator.GetTotalSpreads(-2));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 2)]
    [InlineData(4, 3)]
    [InlineData(8, 5)]
    public void GetTotalSpreads_MatchesExpected(int pageCount, int expectedTotalSpreads)
    {
        Assert.Equal(expectedTotalSpreads, SpreadCalculator.GetTotalSpreads(pageCount));
    }

    [Fact]
    public void GetSpread_ZeroPages_IsCoverPlusCover()
    {
        var spread = SpreadCalculator.GetSpread(pageCount: 0, spreadIndex: 0);

        Assert.Equal(SpreadPanelType.Cover, spread.Left.Type);
        Assert.Equal(SpreadPanelType.Cover, spread.Right.Type);
        Assert.Equal(1, spread.TotalSpreads);
    }

    [Fact]
    public void GetSpread_FourPageBinder_MatchesWorkedExample()
    {
        // spread 0 = cover+p1, spread 1 = p2+p3, spread 2 = p4+back cover
        var spread0 = SpreadCalculator.GetSpread(4, 0);
        Assert.Equal((SpreadPanelType.Cover, (int?)null), (spread0.Left.Type, spread0.Left.PageNumber));
        Assert.Equal((SpreadPanelType.Page, (int?)1), (spread0.Right.Type, spread0.Right.PageNumber));

        var spread1 = SpreadCalculator.GetSpread(4, 1);
        Assert.Equal((SpreadPanelType.Page, (int?)2), (spread1.Left.Type, spread1.Left.PageNumber));
        Assert.Equal((SpreadPanelType.Page, (int?)3), (spread1.Right.Type, spread1.Right.PageNumber));

        var spread2 = SpreadCalculator.GetSpread(4, 2);
        Assert.Equal((SpreadPanelType.Page, (int?)4), (spread2.Left.Type, spread2.Left.PageNumber));
        Assert.Equal((SpreadPanelType.Cover, (int?)null), (spread2.Right.Type, spread2.Right.PageNumber));

        Assert.Equal(3, spread2.TotalSpreads);
    }

    [Fact]
    public void GetSpread_EightPageBinder_LastSpreadIsPageEightPlusBackCover()
    {
        var totalSpreads = SpreadCalculator.GetTotalSpreads(8);
        var lastSpread = SpreadCalculator.GetSpread(8, totalSpreads - 1);

        Assert.Equal((SpreadPanelType.Page, (int?)8), (lastSpread.Left.Type, lastSpread.Left.PageNumber));
        Assert.Equal(SpreadPanelType.Cover, lastSpread.Right.Type);
    }

    [Fact]
    public void GetSpread_OutOfRangeIndex_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SpreadCalculator.GetSpread(4, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => SpreadCalculator.GetSpread(4, -1));
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(2, 1, 2)]
    [InlineData(3, 3, 4)]
    [InlineData(4, 3, 4)]
    [InlineData(7, 7, 8)]
    [InlineData(8, 7, 8)]
    public void GetSheetPair_ReturnsBothFacesOfTheSameLeaf(int pageNumber, int expectedFirst, int expectedSecond)
    {
        var (first, second) = SpreadCalculator.GetSheetPair(pageNumber);
        Assert.Equal(expectedFirst, first);
        Assert.Equal(expectedSecond, second);
    }
}
