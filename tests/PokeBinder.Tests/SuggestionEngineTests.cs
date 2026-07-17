using PokeBinder.Core.Binders;
using Xunit;

namespace PokeBinder.Tests;

public class SuggestionEngineTests
{
    private static SortKey Key(int value) => new(0, "", value, "");
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<CatalogCard>> NoSetCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>>();
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<CatalogCard>> NoNameCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>>();
    private static readonly IReadOnlyDictionary<ThemeKey, IReadOnlyList<CatalogCard>> NoThemeCatalog = new Dictionary<ThemeKey, IReadOnlyList<CatalogCard>>();

    private static CatalogCard Card(
        string id, string name, string setId, DateOnly releaseDate, int number,
        string? rarity = null, string[]? types = null) =>
        new(id, name, setId, releaseDate, Key(number), rarity, types ?? Array.Empty<string>(), Guid.NewGuid());

    private static PlacedCard Placed(Guid slotId, CatalogCard c) =>
        new(slotId, c.CardId, c.Name, c.SetId, c.ReleaseDate, c.NumberSort, c.Rarity, c.Types);

    [Fact]
    public void NextRelease_FiveGengarsInReleaseOrder_SuggestsTheSixth()
    {
        // Mirrors the reported scenario: five Gengars already in the binder, already in release
        // order — the engine should look at the latest one owned and suggest the next chronological
        // release, attaching that same suggestion to every Gengar slot.
        var gengar1 = Card("gengar-1", "Gengar", "set1", new DateOnly(1999, 1, 1), 1);
        var gengar2 = Card("gengar-2", "Gengar", "set2", new DateOnly(2000, 1, 1), 1);
        var gengar3 = Card("gengar-3", "Gengar", "set3", new DateOnly(2001, 1, 1), 1);
        var gengar4 = Card("gengar-4", "Gengar", "set4", new DateOnly(2002, 1, 1), 1);
        var gengar5 = Card("gengar-5", "Gengar", "set5", new DateOnly(2003, 1, 1), 1);
        var gengar6 = Card("gengar-6", "Gengar", "set6", new DateOnly(2004, 1, 1), 1); // not yet placed

        var placedGengars = new[] { gengar1, gengar2, gengar3, gengar4, gengar5 };
        var slotIds = placedGengars.Select(_ => Guid.NewGuid()).ToArray();
        var placed = placedGengars.Select((c, i) => Placed(slotIds[i], c)).ToList();

        var nameCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>>
        {
            ["Gengar"] = new[] { gengar1, gengar2, gengar3, gengar4, gengar5, gengar6 },
        };

        var result = SuggestionEngine.ComputeSuggestions(placed, NoSetCatalog, nameCatalog, NoThemeCatalog);

        Assert.Equal(5, result.Count);
        foreach (var slotId in slotIds)
        {
            var suggestion = Assert.Single(result[slotId]);
            Assert.Equal("gengar-6", suggestion.CardId);
            Assert.Equal(SuggestionReason.NextRelease, suggestion.Reason);
        }
    }

    [Fact]
    public void NextRelease_WhenEveryLaterReleaseAlreadyPlaced_SuggestsNothing()
    {
        var gengar1 = Card("gengar-1", "Gengar", "set1", new DateOnly(1999, 1, 1), 1);

        var slotId = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slotId, gengar1) };

        var nameCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["Gengar"] = new[] { gengar1 } };

        var result = SuggestionEngine.ComputeSuggestions(placed, NoSetCatalog, nameCatalog, NoThemeCatalog);

        Assert.Empty(result);
    }

    [Fact]
    public void NextInSet_SuggestsTheNextSequentialCardNotYetPlaced()
    {
        var card1 = Card("set1-1", "Bulbasaur", "set1", new DateOnly(1999, 1, 1), 1);
        var card2 = Card("set1-2", "Ivysaur", "set1", new DateOnly(1999, 1, 1), 2);
        var card3 = Card("set1-3", "Venusaur", "set1", new DateOnly(1999, 1, 1), 3);

        var slotId = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slotId, card1) };

        var setCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["set1"] = new[] { card1, card2, card3 } };

        var result = SuggestionEngine.ComputeSuggestions(placed, setCatalog, NoNameCatalog, NoThemeCatalog);

        var suggestion = Assert.Single(result[slotId]);
        Assert.Equal("set1-2", suggestion.CardId);
        Assert.Equal(SuggestionReason.NextInSet, suggestion.Reason);
    }

    [Fact]
    public void NextInSet_SkipsAlreadyPlacedCardsToFindTheNextGap()
    {
        var card1 = Card("set1-1", "Bulbasaur", "set1", new DateOnly(1999, 1, 1), 1);
        var card2 = Card("set1-2", "Ivysaur", "set1", new DateOnly(1999, 1, 1), 2);
        var card3 = Card("set1-3", "Venusaur", "set1", new DateOnly(1999, 1, 1), 3);

        var slotId = Guid.NewGuid();
        // Cards 1 and 2 are both already placed; the next-in-set suggestion for card 1's slot should
        // skip past card 2 (already owned) and land on card 3.
        var placed = new List<PlacedCard> { Placed(slotId, card1), Placed(Guid.NewGuid(), card2) };

        var setCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["set1"] = new[] { card1, card2, card3 } };

        var result = SuggestionEngine.ComputeSuggestions(placed, setCatalog, NoNameCatalog, NoThemeCatalog);

        var suggestion = Assert.Single(result[slotId]);
        Assert.Equal("set1-3", suggestion.CardId);
    }

    [Fact]
    public void NextInSet_LastCardInSet_SuggestsNothing()
    {
        var card1 = Card("set1-1", "Bulbasaur", "set1", new DateOnly(1999, 1, 1), 1);

        var slotId = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slotId, card1) };

        var setCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["set1"] = new[] { card1 } };

        var result = SuggestionEngine.ComputeSuggestions(placed, setCatalog, NoNameCatalog, NoThemeCatalog);

        Assert.Empty(result);
    }

    [Fact]
    public void BothRulesFiring_ProduceTwoDistinctSuggestionsOnTheSameSlot()
    {
        var gengar1 = Card("gengar-1", "Gengar", "set1", new DateOnly(1999, 1, 1), 5);
        var gengar2 = Card("gengar-2", "Gengar", "set2", new DateOnly(2000, 1, 1), 1); // next release
        var setNeighbour = Card("set1-6", "Haunter", "set1", new DateOnly(1999, 1, 1), 6); // next in set

        var slotId = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slotId, gengar1) };

        var setCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["set1"] = new[] { gengar1, setNeighbour } };
        var nameCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["Gengar"] = new[] { gengar1, gengar2 } };

        var result = SuggestionEngine.ComputeSuggestions(placed, setCatalog, nameCatalog, NoThemeCatalog);

        var suggestions = result[slotId];
        Assert.Equal(2, suggestions.Count);
        Assert.Contains(suggestions, s => s.CardId == "gengar-2" && s.Reason == SuggestionReason.NextRelease);
        Assert.Contains(suggestions, s => s.CardId == "set1-6" && s.Reason == SuggestionReason.NextInSet);
    }

    [Fact]
    public void SameCardSuggestedByBothRules_AppearsOnlyOnce()
    {
        // gengar2 happens to be both "next in set" (same set, next number) and "next release"
        // (same name, later date) — it should only be reported once for the slot, not twice.
        var gengar1 = Card("gengar-1", "Gengar", "set1", new DateOnly(1999, 1, 1), 1);
        var gengar2 = Card("gengar-2", "Gengar", "set1", new DateOnly(2000, 1, 1), 2);

        var slotId = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slotId, gengar1) };

        var setCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["set1"] = new[] { gengar1, gengar2 } };
        var nameCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["Gengar"] = new[] { gengar1, gengar2 } };

        var result = SuggestionEngine.ComputeSuggestions(placed, setCatalog, nameCatalog, NoThemeCatalog);

        var suggestion = Assert.Single(result[slotId]);
        Assert.Equal("gengar-2", suggestion.CardId);
    }

    [Fact]
    public void SameThemeRarity_AllGrassIllustrationRares_SuggestsAnotherOne()
    {
        var placedGrass1 = Card("ir-1", "Oddish", "set1", new DateOnly(2021, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Grass" });
        var placedGrass2 = Card("ir-2", "Bellsprout", "set2", new DateOnly(2022, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Grass" });
        var candidate = Card("ir-3", "Tangela", "set3", new DateOnly(2020, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Grass" });
        var wrongRarity = Card("ir-4", "Weepinbell", "set4", new DateOnly(2019, 1, 1), 1, rarity: "Ultra Rare", types: new[] { "Grass" });
        var wrongType = Card("ir-5", "Charmander", "set5", new DateOnly(2018, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Fire" });

        var slot1 = Guid.NewGuid();
        var slot2 = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slot1, placedGrass1), Placed(slot2, placedGrass2) };

        var themeCatalog = new Dictionary<ThemeKey, IReadOnlyList<CatalogCard>>
        {
            [new ThemeKey("Illustration Rare", "Grass")] = new[] { placedGrass1, placedGrass2, candidate },
        };

        var result = SuggestionEngine.ComputeSuggestions(placed, NoSetCatalog, NoNameCatalog, themeCatalog);

        foreach (var slotId in new[] { slot1, slot2 })
        {
            var suggestion = Assert.Single(result[slotId]);
            Assert.Equal("ir-3", suggestion.CardId);
            Assert.Equal(SuggestionReason.SameThemeRarity, suggestion.Reason);
        }
    }

    [Fact]
    public void SameThemeRarity_DualTypeCard_MatchesOnEitherType()
    {
        var placedDualType = Card("dual-1", "Bellossom", "set1", new DateOnly(2021, 1, 1), 1, rarity: "Special Illustration Rare", types: new[] { "Grass", "Fairy" });
        var grassCandidate = Card("grass-2", "Sunflora", "set2", new DateOnly(2022, 1, 1), 1, rarity: "Special Illustration Rare", types: new[] { "Grass" });

        var slotId = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slotId, placedDualType) };

        var themeCatalog = new Dictionary<ThemeKey, IReadOnlyList<CatalogCard>>
        {
            [new ThemeKey("Special Illustration Rare", "Grass")] = new[] { placedDualType, grassCandidate },
        };

        var result = SuggestionEngine.ComputeSuggestions(placed, NoSetCatalog, NoNameCatalog, themeCatalog);

        var suggestion = Assert.Single(result[slotId]);
        Assert.Equal("grass-2", suggestion.CardId);
    }
}
