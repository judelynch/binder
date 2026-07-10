using PokeBinder.Core.Cards;
using Xunit;

namespace PokeBinder.Tests;

public class NumberSortKeyCalculatorTests
{
    [Theory]
    [InlineData("4", 0, "", 4, "")]
    [InlineData("4a", 0, "", 4, "A")]
    [InlineData("28a", 0, "", 28, "A")]
    [InlineData("TG12", 1, "TG", 12, "")]
    [InlineData("GG01", 1, "GG", 1, "")]
    [InlineData("SWSH001", 1, "SWSH", 1, "")]
    [InlineData("RC1", 1, "RC", 1, "")]
    [InlineData("SM30a", 1, "SM", 30, "A")]
    [InlineData("A", 2, "A", 0, "")]
    [InlineData("Z", 2, "Z", 0, "")]
    [InlineData("ONE", 2, "ONE", 0, "")]
    [InlineData("?", 3, "?", 0, "")]
    [InlineData("!", 3, "!", 0, "")]
    [InlineData("025/185", 3, "025/185", 0, "")]
    [InlineData("", 3, "", 0, "")]
    public void Compute_ProducesExpectedKey(string number, byte expectedGroup, string expectedPrefix, int expectedValue, string expectedSuffix)
    {
        var key = NumberSortKeyCalculator.Compute(number);

        Assert.Equal(expectedGroup, key.Group);
        Assert.Equal(expectedPrefix, key.Prefix);
        Assert.Equal(expectedValue, key.Value);
        Assert.Equal(expectedSuffix, key.Suffix);
    }

    [Fact]
    public void Compute_OrdersRealBw11StyleSequenceCorrectly()
    {
        // Legendary Treasures (bw11): 1..115 numeric, then RC1..RC25 Radiant Collection.
        var numbers = new[] { "RC2", "28", "1", "RC1", "2", "115", "29" };

        var ordered = numbers
            .Select(n => (Number: n, Key: NumberSortKeyCalculator.Compute(n)))
            .OrderBy(x => x.Key.Group)
            .ThenBy(x => x.Key.Prefix, StringComparer.Ordinal)
            .ThenBy(x => x.Key.Value)
            .ThenBy(x => x.Key.Suffix, StringComparer.Ordinal)
            .Select(x => x.Number)
            .ToArray();

        Assert.Equal(new[] { "1", "2", "28", "29", "115", "RC1", "RC2" }, ordered);
    }

    [Fact]
    public void Compute_PlacesNumericSuffixCardImmediatelyAfterItsBaseNumber()
    {
        var numbers = new[] { "29", "28a", "28" };

        var ordered = numbers
            .Select(n => (Number: n, Key: NumberSortKeyCalculator.Compute(n)))
            .OrderBy(x => x.Key.Group)
            .ThenBy(x => x.Key.Prefix, StringComparer.Ordinal)
            .ThenBy(x => x.Key.Value)
            .ThenBy(x => x.Key.Suffix, StringComparer.Ordinal)
            .Select(x => x.Number)
            .ToArray();

        Assert.Equal(new[] { "28", "28a", "29" }, ordered);
    }
}
