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
public record CatalogCard(string CardId, string Name, string SetId, string Supertype, DateOnly ReleaseDate, SortKey NumberSort, string? Rarity, IReadOnlyList<string> Types, Guid DefaultVariantId);

/// <summary>A card currently sitting in a binder slot.</summary>
public record PlacedCard(Guid SlotId, string CardId, string Name, string SetId, string Supertype, DateOnly ReleaseDate, SortKey NumberSort, string? Rarity, IReadOnlyList<string> Types);

/// <summary>A (rarity, elemental type) combination, e.g. ("Illustration Rare", "Grass") - a Pokémon-flavoured theme.</summary>
public record ThemeKey(string Rarity, string Type);

/// <summary>A (rarity, supertype) combination, e.g. ("Ultra Rare", "Trainer") - a theme for non-Pokémon cards, which have no elemental type to match on.</summary>
public record RaritySupertypeKey(string Rarity, string Supertype);

public enum SuggestionReason
{
    NextInSet,
    PrevInSet,
    NextRelease,
    SameThemeRarity,
}

public record CardSuggestion(string CardId, string Name, string SetId, DateOnly ReleaseDate, Guid DefaultVariantId, SuggestionReason Reason);

/// <summary>
/// Pure suggestion computation, no EF/DB access. Rather than running every heuristic independently
/// against every placed card (which is what made suggestions feel random - a set-neighbour here, an
/// unrelated same-rarity card there, with no throughline), this first decides what the WHOLE BINDER
/// looks like it's "about" - a set to complete, a Pokémon to collect every print of, or a rarity+
/// theme to round out - and then generates suggestions from that one story only.
///
/// The binder's dominant pattern is whichever category (set / name / rarity+type / rarity+supertype)
/// has the single largest group of placed cards sharing that key, no fixed threshold - even a
/// three-card binder gets a themed reading. Ties break in a fixed priority (set > name > rarity+type
/// > rarity+supertype): a shared set is the most deliberate signal, a shared name next, and between
/// the two rarity-based combos the narrower type-matched one wins a tie over the broader supertype
/// one (see ComputeSuggestions for why they tie so often).
/// </summary>
public static class SuggestionEngine
{
    private enum ThemeCategory
    {
        Set,
        Name,
        RaritySupertype,
        RarityType,
    }

    public static IReadOnlyDictionary<Guid, IReadOnlyList<CardSuggestion>> ComputeSuggestions(
        IReadOnlyList<PlacedCard> placedCards,
        IReadOnlyDictionary<string, IReadOnlyList<CatalogCard>> setCatalog,
        IReadOnlyDictionary<string, IReadOnlyList<CatalogCard>> nameCatalog,
        IReadOnlyDictionary<ThemeKey, IReadOnlyList<CatalogCard>> rarityTypeCatalog,
        IReadOnlyDictionary<RaritySupertypeKey, IReadOnlyList<CatalogCard>> raritySupertypeCatalog)
    {
        var result = new Dictionary<Guid, List<CardSuggestion>>();
        if (placedCards.Count == 0)
        {
            return result.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<CardSuggestion>)kv.Value);
        }

        var placedCardIds = new HashSet<string>(placedCards.Select(p => p.CardId));

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

        var bestSetSize = placedCards.GroupBy(p => p.SetId).Select(g => g.Count()).DefaultIfEmpty(0).Max();
        var bestNameSize = placedCards.GroupBy(p => p.Name).Select(g => g.Count()).DefaultIfEmpty(0).Max();
        var bestRaritySupertypeSize = placedCards.Where(p => p.Rarity != null)
            .GroupBy(p => new RaritySupertypeKey(p.Rarity!, p.Supertype))
            .Select(g => g.Count()).DefaultIfEmpty(0).Max();
        var bestRarityTypeSize = placedCards.Where(p => p.Rarity != null)
            .SelectMany(p => p.Types.Select(t => new ThemeKey(p.Rarity!, t)))
            .GroupBy(k => k)
            .Select(g => g.Count()).DefaultIfEmpty(0).Max();

        // RarityType is tried before RaritySupertype on a tie: any monotype rarity-block (every
        // placed card sharing both the same rarity AND the same single element type) ties them
        // exactly, since RaritySupertype("X","Pokémon") and RarityType("X","Grass") then cover the
        // same cards - the narrower, type-matched grouping is the more relevant story to suggest
        // from. RaritySupertype only wins outright when its count is genuinely larger (multiple
        // types sharing a rarity, or non-Pokémon cards, which have no element type to group by at all).
        var winner = new[]
        {
            (Category: ThemeCategory.Set, Size: bestSetSize),
            (Category: ThemeCategory.Name, Size: bestNameSize),
            (Category: ThemeCategory.RarityType, Size: bestRarityTypeSize),
            (Category: ThemeCategory.RaritySupertype, Size: bestRaritySupertypeSize),
        }.OrderByDescending(c => c.Size).First().Category;

        switch (winner)
        {
            case ThemeCategory.Set:
                ApplySetTheme(placedCards, placedCardIds, setCatalog, AddSuggestion);
                break;
            case ThemeCategory.Name:
                ApplyNameTheme(placedCards, placedCardIds, nameCatalog, AddSuggestion);
                break;
            case ThemeCategory.RaritySupertype:
                ApplyRaritySupertypeTheme(placedCards, placedCardIds, raritySupertypeCatalog, AddSuggestion);
                break;
            case ThemeCategory.RarityType:
                ApplyRarityTypeTheme(placedCards, placedCardIds, rarityTypeCatalog, AddSuggestion);
                break;
        }

        return result.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<CardSuggestion>)kv.Value);
    }

    /// <summary>This binder is (mostly) one set: for every placed card, suggest its immediate missing neighbour on both sides, e.g. card 123 placed -> suggest 122 and 124.</summary>
    private static void ApplySetTheme(
        IReadOnlyList<PlacedCard> placedCards, HashSet<string> placedCardIds,
        IReadOnlyDictionary<string, IReadOnlyList<CatalogCard>> setCatalog, Action<Guid, CardSuggestion> add)
    {
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
                add(placed.SlotId, new CardSuggestion(next.CardId, next.Name, next.SetId, next.ReleaseDate, next.DefaultVariantId, SuggestionReason.NextInSet));
            }

            var prev = setCards
                .Where(c => c.NumberSort.CompareTo(placed.NumberSort) < 0 && !placedCardIds.Contains(c.CardId))
                .OrderByDescending(c => c.NumberSort)
                .FirstOrDefault();
            if (prev is not null)
            {
                add(placed.SlotId, new CardSuggestion(prev.CardId, prev.Name, prev.SetId, prev.ReleaseDate, prev.DefaultVariantId, SuggestionReason.PrevInSet));
            }
        }
    }

    /// <summary>This binder is (mostly) one Pokémon: for every name-group of placed cards, suggest the next chronological print not yet placed, attached to every slot in that group.</summary>
    private static void ApplyNameTheme(
        IReadOnlyList<PlacedCard> placedCards, HashSet<string> placedCardIds,
        IReadOnlyDictionary<string, IReadOnlyList<CatalogCard>> nameCatalog, Action<Guid, CardSuggestion> add)
    {
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
                add(placed.SlotId, new CardSuggestion(next.CardId, next.Name, next.SetId, next.ReleaseDate, next.DefaultVariantId, SuggestionReason.NextRelease));
            }
        }
    }

    /// <summary>This binder is (mostly) one rarity+supertype combo (e.g. Ultra Rare Trainers): suggest another card matching it, attached to every placed card that matches.</summary>
    private static void ApplyRaritySupertypeTheme(
        IReadOnlyList<PlacedCard> placedCards, HashSet<string> placedCardIds,
        IReadOnlyDictionary<RaritySupertypeKey, IReadOnlyList<CatalogCard>> raritySupertypeCatalog, Action<Guid, CardSuggestion> add)
    {
        foreach (var group in placedCards.Where(p => p.Rarity != null).GroupBy(p => new RaritySupertypeKey(p.Rarity!, p.Supertype)))
        {
            if (!raritySupertypeCatalog.TryGetValue(group.Key, out var themeCards))
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

            foreach (var placed in group)
            {
                add(placed.SlotId, new CardSuggestion(next.CardId, next.Name, next.SetId, next.ReleaseDate, next.DefaultVariantId, SuggestionReason.SameThemeRarity));
            }
        }
    }

    /// <summary>This binder is (mostly) one rarity+elemental-type combo (e.g. Illustration Rare Grass-types): suggest another card matching it. Dual-type cards match on either type.</summary>
    private static void ApplyRarityTypeTheme(
        IReadOnlyList<PlacedCard> placedCards, HashSet<string> placedCardIds,
        IReadOnlyDictionary<ThemeKey, IReadOnlyList<CatalogCard>> rarityTypeCatalog, Action<Guid, CardSuggestion> add)
    {
        var processedKeys = new HashSet<ThemeKey>();
        foreach (var placed in placedCards)
        {
            if (placed.Rarity is null)
            {
                continue;
            }

            foreach (var type in placed.Types)
            {
                var key = new ThemeKey(placed.Rarity, type);
                if (!processedKeys.Add(key) || !rarityTypeCatalog.TryGetValue(key, out var themeCards))
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
                    add(match.SlotId, new CardSuggestion(next.CardId, next.Name, next.SetId, next.ReleaseDate, next.DefaultVariantId, SuggestionReason.SameThemeRarity));
                }
            }
        }
    }
}
