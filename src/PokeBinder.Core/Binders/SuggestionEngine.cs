namespace PokeBinder.Core.Binders;

public record SortKey(byte Group, string Prefix, int Value, string Suffix) : IComparable<SortKey>
{
    public int CompareTo(SortKey? other)
    {
        if (other is null)
        {
            return 1;
        }

        var byGroup = Group.CompareTo(other.Group);
        if (byGroup != 0)
        {
            return byGroup;
        }

        var byPrefix = string.CompareOrdinal(Prefix, other.Prefix);
        if (byPrefix != 0)
        {
            return byPrefix;
        }

        var byValue = Value.CompareTo(other.Value);
        return byValue != 0 ? byValue : string.CompareOrdinal(Suffix, other.Suffix);
    }
}

/// <summary>A card already in the catalog, available as a suggestion candidate.</summary>
public record CatalogCard(string CardId, string Name, string SetId, DateOnly ReleaseDate, SortKey NumberSort, string? Rarity, IReadOnlyList<string> Types, Guid DefaultVariantId);

/// <summary>A card currently sitting in a binder slot.</summary>
public record PlacedCard(Guid SlotId, string CardId, string Name, string SetId, DateOnly ReleaseDate, SortKey NumberSort, string? Rarity, IReadOnlyList<string> Types);

/// <summary>A (rarity, type) combination, e.g. ("Illustration Rare", "Grass") — the "theme" a run of cards might be collecting.</summary>
public record ThemeKey(string Rarity, string Type);

public enum SuggestionReason
{
    NextInSet,
    NextRelease,
    SameThemeRarity,
}

public record CardSuggestion(string CardId, string Name, string SetId, DateOnly ReleaseDate, Guid DefaultVariantId, SuggestionReason Reason);

/// <summary>
/// Pure suggestion computation, no EF/DB access: given what's already in a binder and the relevant
/// slices of the catalog, finds two kinds of "connecting card" per slot —
///   - NextInSet: the next sequential card number in the same set that isn't already placed.
///   - NextRelease: for every card sharing a name with something placed (e.g. five Gengars), the next
///     chronological release of that name that isn't already placed, attached to every one of those slots.
/// A card already sitting in ANY slot in the binder is never suggested again.
/// </summary>
public static class SuggestionEngine
{
    public static IReadOnlyDictionary<Guid, IReadOnlyList<CardSuggestion>> ComputeSuggestions(
        IReadOnlyList<PlacedCard> placedCards,
        IReadOnlyDictionary<string, IReadOnlyList<CatalogCard>> setCatalog,
        IReadOnlyDictionary<string, IReadOnlyList<CatalogCard>> nameCatalog,
        IReadOnlyDictionary<ThemeKey, IReadOnlyList<CatalogCard>> themeCatalog)
    {
        var placedCardIds = new HashSet<string>(placedCards.Select(p => p.CardId));
        var result = new Dictionary<Guid, List<CardSuggestion>>();

        void AddSuggestion(Guid slotId, CardSuggestion suggestion)
        {
            if (!result.TryGetValue(slotId, out var list))
            {
                list = new List<CardSuggestion>();
                result[slotId] = list;
            }

            if (!list.Any(s => s.CardId == suggestion.CardId))
            {
                list.Add(suggestion);
            }
        }

        foreach (var placed in placedCards)
        {
            if (!setCatalog.TryGetValue(placed.SetId, out var setCards))
            {
                continue;
            }

            var next = setCards
                .Where(c => c.NumberSort.CompareTo(placed.NumberSort) > 0 && !placedCardIds.Contains(c.CardId))
                .OrderBy(c => c.NumberSort)
                .FirstOrDefault();

            if (next is not null)
            {
                AddSuggestion(placed.SlotId, new CardSuggestion(next.CardId, next.Name, next.SetId, next.ReleaseDate, next.DefaultVariantId, SuggestionReason.NextInSet));
            }
        }

        foreach (var group in placedCards.GroupBy(p => p.Name))
        {
            if (!nameCatalog.TryGetValue(group.Key, out var nameCards))
            {
                continue;
            }

            var latestReleaseDate = group.Max(p => p.ReleaseDate);
            var next = nameCards
                .Where(c => c.ReleaseDate > latestReleaseDate && !placedCardIds.Contains(c.CardId))
                .OrderBy(c => c.ReleaseDate).ThenBy(c => c.SetId).ThenBy(c => c.NumberSort)
                .FirstOrDefault();

            if (next is null)
            {
                continue;
            }

            foreach (var placed in group)
            {
                AddSuggestion(placed.SlotId, new CardSuggestion(next.CardId, next.Name, next.SetId, next.ReleaseDate, next.DefaultVariantId, SuggestionReason.NextRelease));
            }
        }

        var processedThemeKeys = new HashSet<ThemeKey>();
        foreach (var placed in placedCards)
        {
            if (placed.Rarity is null)
            {
                continue;
            }

            foreach (var type in placed.Types)
            {
                var key = new ThemeKey(placed.Rarity, type);
                if (!processedThemeKeys.Add(key) || !themeCatalog.TryGetValue(key, out var themeCards))
                {
                    continue;
                }

                var next = themeCards
                    .Where(c => !placedCardIds.Contains(c.CardId))
                    .OrderBy(c => c.ReleaseDate).ThenBy(c => c.SetId).ThenBy(c => c.NumberSort)
                    .FirstOrDefault();

                if (next is null)
                {
                    continue;
                }

                foreach (var match in placedCards.Where(p => p.Rarity == key.Rarity && p.Types.Contains(key.Type)))
                {
                    AddSuggestion(match.SlotId, new CardSuggestion(next.CardId, next.Name, next.SetId, next.ReleaseDate, next.DefaultVariantId, SuggestionReason.SameThemeRarity));
                }
            }
        }

        return result.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<CardSuggestion>)kv.Value);
    }
}
