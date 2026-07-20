using PokeBinder.Core.Binders;
using Xunit;

namespace PokeBinder.Tests;

public class SuggestionEngineTests
{
    private static SortKey Key(int value) => new(0, "", value, "");
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<CatalogCard>> NoSetCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>>();
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<CatalogCard>> NoNameCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>>();
    private static readonly IReadOnlyDictionary<ThemeKey, IReadOnlyList<CatalogCard>> NoRarityTypeCatalog = new Dictionary<ThemeKey, IReadOnlyList<CatalogCard>>();
    private static readonly IReadOnlyDictionary<RaritySupertypeKey, IReadOnlyList<CatalogCard>> NoRaritySupertypeCatalog = new Dictionary<RaritySupertypeKey, IReadOnlyList<CatalogCard>>();

    private static CatalogCard Card(
        string id, string name, string setId, DateOnly releaseDate, int number,
        string supertype = "Pokémon", string? rarity = null, string[]? types = null) =>
        new(id, name, setId, supertype, releaseDate, Key(number), rarity, types ?? Array.Empty<string>(), Guid.NewGuid());

    private static PlacedCard Placed(Guid slotId, CatalogCard c) =>
        new(slotId, c.CardId, c.Name, c.SetId, c.Supertype, c.ReleaseDate, c.NumberSort, c.Rarity, c.Types);

    private static IReadOnlyDictionary<Guid, IReadOnlyList<CardSuggestion>> Compute(
        List<PlacedCard> placed,
        IReadOnlyDictionary<string, IReadOnlyList<CatalogCard>>? setCatalog = null,
        IReadOnlyDictionary<string, IReadOnlyList<CatalogCard>>? nameCatalog = null,
        IReadOnlyDictionary<ThemeKey, IReadOnlyList<CatalogCard>>? rarityTypeCatalog = null,
        IReadOnlyDictionary<RaritySupertypeKey, IReadOnlyList<CatalogCard>>? raritySupertypeCatalog = null) =>
        SuggestionEngine.ComputeSuggestions(
            placed,
            setCatalog ?? NoSetCatalog,
            nameCatalog ?? NoNameCatalog,
            rarityTypeCatalog ?? NoRarityTypeCatalog,
            raritySupertypeCatalog ?? NoRaritySupertypeCatalog);

    [Fact]
    public void EmptyBinder_ReturnsNoSuggestions()
    {
        var result = Compute(new List<PlacedCard>());
        Assert.Empty(result);
    }

    [Fact]
    public void SetTheme_Wins_SuggestsBothNeighboursForAGapInTheMiddle()
    {
        // Three of five slots are in the same set (Bulbasaur/Venusaur/Blastoise at 1/3/5), and share
        // no name/rarity/type - Set is the only category with a group larger than 1, so it wins.
        var c1 = Card("s-1", "Bulbasaur", "setA", new DateOnly(1999, 1, 1), 1);
        var c2 = Card("s-2", "Ivysaur", "setA", new DateOnly(1999, 1, 1), 2); // gap - not placed
        var c3 = Card("s-3", "Venusaur", "setA", new DateOnly(1999, 1, 1), 3);
        var c4 = Card("s-4", "Charmander", "setA", new DateOnly(1999, 1, 1), 4); // gap - not placed
        var c5 = Card("s-5", "Blastoise", "setA", new DateOnly(1999, 1, 1), 5);

        var slot1 = Guid.NewGuid();
        var slot3 = Guid.NewGuid();
        var slot5 = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slot1, c1), Placed(slot3, c3), Placed(slot5, c5) };

        var setCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["setA"] = new[] { c1, c2, c3, c4, c5 } };

        var result = Compute(placed, setCatalog: setCatalog);

        var slot3Suggestions = result[slot3];
        Assert.Equal(2, slot3Suggestions.Count);
        Assert.Contains(slot3Suggestions, s => s.CardId == "s-2" && s.Reason == SuggestionReason.PrevInSet);
        Assert.Contains(slot3Suggestions, s => s.CardId == "s-4" && s.Reason == SuggestionReason.NextInSet);
    }

    [Fact]
    public void SetTheme_FirstCardInSet_OnlySuggestsForward()
    {
        var c1 = Card("s-1", "Bulbasaur", "setA", new DateOnly(1999, 1, 1), 1);
        var c2 = Card("s-2", "Ivysaur", "setA", new DateOnly(1999, 1, 1), 2);
        var c3 = Card("s-3", "Venusaur", "setA", new DateOnly(1999, 1, 1), 3);

        var slot1 = Guid.NewGuid();
        var slot3 = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slot1, c1), Placed(slot3, c3) };

        var setCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["setA"] = new[] { c1, c2, c3 } };

        var result = Compute(placed, setCatalog: setCatalog);

        var suggestion = Assert.Single(result[slot1]);
        Assert.Equal("s-2", suggestion.CardId);
        Assert.Equal(SuggestionReason.NextInSet, suggestion.Reason);
    }

    [Fact]
    public void NameTheme_Wins_SuggestsNextReleaseForEveryPrintInTheGroup()
    {
        // Three Gengars from three different sets - no set/rarity/type is shared by more than one
        // card, so Name (group of 3) wins over Set (max 1 each).
        var gengar1 = Card("gengar-1", "Gengar", "set1", new DateOnly(1999, 1, 1), 1);
        var gengar2 = Card("gengar-2", "Gengar", "set2", new DateOnly(2000, 1, 1), 1);
        var gengar3 = Card("gengar-3", "Gengar", "set3", new DateOnly(2001, 1, 1), 1);
        var gengar4 = Card("gengar-4", "Gengar", "set4", new DateOnly(2002, 1, 1), 1); // not placed

        var slotIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var placed = new[] { gengar1, gengar2, gengar3 }.Select((c, i) => Placed(slotIds[i], c)).ToList();

        var nameCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["Gengar"] = new[] { gengar1, gengar2, gengar3, gengar4 } };

        var result = Compute(placed, nameCatalog: nameCatalog);

        Assert.Equal(3, result.Count);
        foreach (var slotId in slotIds)
        {
            var suggestion = Assert.Single(result[slotId]);
            Assert.Equal("gengar-4", suggestion.CardId);
            Assert.Equal(SuggestionReason.NextRelease, suggestion.Reason);
        }
    }

    [Fact]
    public void RaritySupertypeTheme_Wins_ForNonPokemonCardsWithNoElementType()
    {
        // Three Ultra Rare Trainers from different sets, all with empty Types (Trainers have no
        // element type) - RarityType can never form a group for these, so RaritySupertype (group of
        // 3) is the only real candidate and wins outright.
        var bill = Card("t-1", "Bill", "set1", new DateOnly(2019, 1, 1), 10, supertype: "Trainer", rarity: "Ultra Rare");
        var oak = Card("t-2", "Oak", "set2", new DateOnly(2020, 1, 1), 11, supertype: "Trainer", rarity: "Ultra Rare");
        var marnie = Card("t-3", "Marnie", "set3", new DateOnly(2021, 1, 1), 12, supertype: "Trainer", rarity: "Ultra Rare");
        var missing = Card("t-4", "Cynthia", "set4", new DateOnly(2018, 1, 1), 13, supertype: "Trainer", rarity: "Ultra Rare"); // not placed

        var slotIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var placed = new[] { bill, oak, marnie }.Select((c, i) => Placed(slotIds[i], c)).ToList();

        var raritySupertypeCatalog = new Dictionary<RaritySupertypeKey, IReadOnlyList<CatalogCard>>
        {
            [new RaritySupertypeKey("Ultra Rare", "Trainer")] = new[] { bill, oak, marnie, missing },
        };

        var result = Compute(placed, raritySupertypeCatalog: raritySupertypeCatalog);

        foreach (var slotId in slotIds)
        {
            var suggestion = Assert.Single(result[slotId]);
            Assert.Equal("t-4", suggestion.CardId);
            Assert.Equal(SuggestionReason.SameThemeRarity, suggestion.Reason);
        }
    }

    [Fact]
    public void RarityTypeTheme_WinsTieOverRaritySupertype_WhenEveryCardSharesTheSameSingleType()
    {
        // Every placed card shares both (rarity, supertype="Pokémon") AND (rarity, type="Grass") -
        // an exact tie between the two rarity-based categories. The narrower, type-matched grouping
        // should win so the suggestion stays Grass-relevant instead of widening to any Pokémon.
        var grass1 = Card("ir-1", "Oddish", "set1", new DateOnly(2021, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Grass" });
        var grass2 = Card("ir-2", "Bellsprout", "set2", new DateOnly(2022, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Grass" });
        var grassCandidate = Card("ir-3", "Tangela", "set3", new DateOnly(2020, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Grass" });
        var fireCandidate = Card("ir-4", "Charmander", "set4", new DateOnly(2019, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Fire" });

        var slot1 = Guid.NewGuid();
        var slot2 = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slot1, grass1), Placed(slot2, grass2) };

        var rarityTypeCatalog = new Dictionary<ThemeKey, IReadOnlyList<CatalogCard>>
        {
            [new ThemeKey("Illustration Rare", "Grass")] = new[] { grass1, grass2, grassCandidate },
        };
        // Deliberately over-inclusive, like the real controller would build it - RaritySupertype
        // catalog contains a same-rarity Fire card too. If RaritySupertype won the tie, this Fire
        // card could get suggested instead of the Grass one.
        var raritySupertypeCatalog = new Dictionary<RaritySupertypeKey, IReadOnlyList<CatalogCard>>
        {
            [new RaritySupertypeKey("Illustration Rare", "Pokémon")] = new[] { grass1, grass2, grassCandidate, fireCandidate },
        };

        var result = Compute(placed, rarityTypeCatalog: rarityTypeCatalog, raritySupertypeCatalog: raritySupertypeCatalog);

        foreach (var slotId in new[] { slot1, slot2 })
        {
            var suggestion = Assert.Single(result[slotId]);
            Assert.Equal("ir-3", suggestion.CardId);
            Assert.Equal(SuggestionReason.SameThemeRarity, suggestion.Reason);
        }
    }

    [Fact]
    public void RaritySupertypeTheme_WinsOutright_WhenItsCountExceedsRarityType()
    {
        // Five same-rarity Pokémon of three DIFFERENT types - the biggest RarityType group is only
        // 2 (Grass), but RaritySupertype("Illustration Rare","Pokémon") covers all 5. RaritySupertype
        // should win on its own genuinely larger count, not just a tie-break.
        var grass1 = Card("ir-1", "Oddish", "set1", new DateOnly(2021, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Grass" });
        var grass2 = Card("ir-2", "Bellsprout", "set2", new DateOnly(2022, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Grass" });
        var fire1 = Card("ir-3", "Charmander", "set3", new DateOnly(2019, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Fire" });
        var water1 = Card("ir-4", "Squirtle", "set4", new DateOnly(2018, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Water" });
        var electric1 = Card("ir-5", "Pikachu", "set5", new DateOnly(2017, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Electric" });
        var candidate = Card("ir-6", "Psyduck", "set6", new DateOnly(2023, 1, 1), 1, rarity: "Illustration Rare", types: new[] { "Water" }); // not placed

        var slots = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        var placedCards = new[] { grass1, grass2, fire1, water1, electric1 };
        var placed = placedCards.Select((c, i) => Placed(slots[i], c)).ToList();

        var raritySupertypeCatalog = new Dictionary<RaritySupertypeKey, IReadOnlyList<CatalogCard>>
        {
            [new RaritySupertypeKey("Illustration Rare", "Pokémon")] = placedCards.Append(candidate).ToArray(),
        };

        var result = Compute(placed, raritySupertypeCatalog: raritySupertypeCatalog);

        // Every placed card should get the same suggestion, since they all share the winning key.
        Assert.All(slots, slotId => Assert.Equal("ir-6", Assert.Single(result[slotId]).CardId));
    }

    [Fact]
    public void TieBreak_SetBeatsNameAndRarityCategories_WhenGroupSizesAreEqual()
    {
        // Two cards share a set; the same two cards ALSO happen to share a name and a rarity+type -
        // every category ties at group size 2. Set should win per the fixed priority order.
        var c1 = Card("s-1", "Ditto", "setA", new DateOnly(2019, 1, 1), 1, rarity: "Rare", types: new[] { "Colorless" });
        var c2 = Card("s-2", "Ditto", "setA", new DateOnly(2020, 1, 1), 2, rarity: "Rare", types: new[] { "Colorless" });
        var setNeighbour = Card("s-3", "Eevee", "setA", new DateOnly(2019, 1, 1), 3); // not placed

        var slot1 = Guid.NewGuid();
        var slot2 = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slot1, c1), Placed(slot2, c2) };

        var setCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["setA"] = new[] { c1, c2, setNeighbour } };
        // Populate the other catalogs too, so if the wrong category won it would produce a
        // different, detectable suggestion rather than silently no-op.
        var nameCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>>
        {
            ["Ditto"] = new[] { c1, c2, Card("s-9", "Ditto", "setZ", new DateOnly(2021, 1, 1), 1) },
        };
        var rarityTypeCatalog = new Dictionary<ThemeKey, IReadOnlyList<CatalogCard>>
        {
            [new ThemeKey("Rare", "Colorless")] = new[] { c1, c2, Card("s-8", "Porygon", "setY", new DateOnly(2021, 1, 1), 1, rarity: "Rare", types: new[] { "Colorless" }) },
        };

        var result = Compute(placed, setCatalog: setCatalog, nameCatalog: nameCatalog, rarityTypeCatalog: rarityTypeCatalog);

        var suggestion = Assert.Single(result[slot2]);
        Assert.Equal("s-3", suggestion.CardId);
        Assert.Equal(SuggestionReason.NextInSet, suggestion.Reason);
    }

    [Fact]
    public void SinglePlacedCard_DefaultsToSetTheme()
    {
        var c1 = Card("s-1", "Bulbasaur", "setA", new DateOnly(1999, 1, 1), 1);
        var c2 = Card("s-2", "Ivysaur", "setA", new DateOnly(1999, 1, 1), 2);

        var slotId = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slotId, c1) };

        var setCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["setA"] = new[] { c1, c2 } };

        var result = Compute(placed, setCatalog: setCatalog);

        var suggestion = Assert.Single(result[slotId]);
        Assert.Equal("s-2", suggestion.CardId);
        Assert.Equal(SuggestionReason.NextInSet, suggestion.Reason);
    }

    [Fact]
    public void SetTheme_NoNeighboursLeftInSet_SuggestsNothing()
    {
        var c1 = Card("s-1", "Bulbasaur", "setA", new DateOnly(1999, 1, 1), 1);

        var slotId = Guid.NewGuid();
        var placed = new List<PlacedCard> { Placed(slotId, c1) };

        var setCatalog = new Dictionary<string, IReadOnlyList<CatalogCard>> { ["setA"] = new[] { c1 } };

        var result = Compute(placed, setCatalog: setCatalog);

        Assert.Empty(result);
    }
}
