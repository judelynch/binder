using System.Text.RegularExpressions;

namespace PokeBinder.Core.Cards;

public readonly record struct NumberSortKey(byte Group, string Prefix, int Value, string Suffix);

/// <summary>
/// Computes an orderable sort key from a card's raw printed number string.
///
/// Validated against every distinct number shape found in the full
/// PokemonTCG/pokemon-tcg-data dataset (20,324 cards / 173 sets, checked
/// 2026-07-10): plain numerics ("4"), numeric+letter suffixes ("28a", alt
/// art sharing a base number), letter-prefix+digits ("TG12", "RC1", "SV001",
/// "SWSH001"), pure-letter runs ("A".."Z", "ONE"), and a handful of
/// unparseable one-offs ("!", "?"). No real card number contains a slash;
/// "025/185"-style display combines the number with the set's printedTotal
/// and is never a literal field value.
/// </summary>
public static class NumberSortKeyCalculator
{
    private static readonly Regex NumericLed = new(@"^([0-9]+)([A-Za-z]*)$", RegexOptions.Compiled);
    private static readonly Regex PrefixedDigits = new(@"^([A-Za-z]+)([0-9]+)([A-Za-z]*)$", RegexOptions.Compiled);
    private static readonly Regex PureAlpha = new(@"^[A-Za-z]+$", RegexOptions.Compiled);

    public static NumberSortKey Compute(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return new NumberSortKey(3, string.Empty, 0, string.Empty);
        }

        var trimmed = number.Trim();

        var numericMatch = NumericLed.Match(trimmed);
        if (numericMatch.Success)
        {
            var value = int.Parse(numericMatch.Groups[1].Value);
            var suffix = numericMatch.Groups[2].Value.ToUpperInvariant();
            return new NumberSortKey(0, string.Empty, value, suffix);
        }

        var prefixedMatch = PrefixedDigits.Match(trimmed);
        if (prefixedMatch.Success)
        {
            var prefix = prefixedMatch.Groups[1].Value.ToUpperInvariant();
            var value = int.Parse(prefixedMatch.Groups[2].Value);
            var suffix = prefixedMatch.Groups[3].Value.ToUpperInvariant();
            return new NumberSortKey(1, prefix, value, suffix);
        }

        if (PureAlpha.IsMatch(trimmed))
        {
            return new NumberSortKey(2, trimmed.ToUpperInvariant(), 0, string.Empty);
        }

        return new NumberSortKey(3, trimmed.ToUpperInvariant(), 0, string.Empty);
    }
}
