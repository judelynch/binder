using PokeBinder.Core.Cards;
using PokeBinder.Infrastructure;

namespace PokeBinder.Tests;

/// <summary>Seeds a small set of cards with realistic filterable data (types, subtypes, HP, rarity) directly, mirroring what CardDataImporter would produce, for search-endpoint tests.</summary>
public static class SearchCardFixture
{
    public static async Task SeedAsync(PokeBinderDbContext db)
    {
        var set = new Set
        {
            Id = "search-set",
            Name = "Search Test Set",
            Series = "Search Series",
            PrintedTotal = 5,
            Total = 5,
            ReleaseDate = new DateOnly(2022, 6, 1),
            UpdatedAt = DateTime.UtcNow
        };
        db.Sets.Add(set);

        var normalVariantType = new VariantType { Id = Guid.NewGuid(), Name = "Normal" };
        var reverseHoloVariantType = new VariantType { Id = Guid.NewGuid(), Name = "Reverse Holo" };
        db.VariantTypes.AddRange(normalVariantType, reverseHoloVariantType);

        AddCard(db, set.Id, normalVariantType.Id, "search-1", "Pikachu", "Pokémon", "Basic",
            types: new[] { "Lightning" }, hp: 60, rarity: "Common", weaknessType: "Fighting", resistanceType: null, retreatCost: 1, artist: "Mitsuhiro Arita");

        AddCard(db, set.Id, normalVariantType.Id, "search-2", "Charizard", "Pokémon", "Stage 2",
            types: new[] { "Fire" }, hp: 180, rarity: "Rare Holo", weaknessType: "Water", resistanceType: null, retreatCost: 3, artist: "Mitsuhiro Arita",
            abilities: new[] { new Ability("Energy Burn", "All Fire-type attacks do 20 more damage.", "Poké-Power") },
            attacks: new[] { new Attack("Fire Spin", new[] { "Fire", "Fire", "Fire", "Fire" }, 4, "100", "Discard 2 Energy from this Pokémon.") },
            retreatCostList: new[] { "Colorless", "Colorless", "Colorless" });

        AddCard(db, set.Id, normalVariantType.Id, "search-3", "Blastoise", "Pokémon", "Stage 2",
            types: new[] { "Water" }, hp: 180, rarity: "Rare Holo", weaknessType: "Lightning", resistanceType: null, retreatCost: 3, artist: "Ken Sugimori");

        AddCard(db, set.Id, normalVariantType.Id, "search-4", "Bill", "Trainer", "Supporter",
            types: Array.Empty<string>(), hp: null, rarity: "Common", weaknessType: null, resistanceType: null, retreatCost: null, artist: "Ken Sugimori");

        AddCard(db, set.Id, normalVariantType.Id, "search-5", "Fire Energy", "Energy", "Basic",
            types: Array.Empty<string>(), hp: null, rarity: "Common", weaknessType: null, resistanceType: null, retreatCost: null, artist: null);

        // Only Charizard also comes in Reverse Holo, so filtering by variant type can be tested meaningfully.
        db.CardVariants.Add(new CardVariant { Id = Guid.NewGuid(), CardId = "search-2", VariantTypeId = reverseHoloVariantType.Id });

        await db.SaveChangesAsync();
    }

    private static void AddCard(
        PokeBinderDbContext db, string setId, Guid normalVariantTypeId, string id, string name, string supertype, string subtype,
        string[] types, int? hp, string rarity, string? weaknessType, string? resistanceType, int? retreatCost, string? artist,
        IReadOnlyList<Ability>? abilities = null, IReadOnlyList<Attack>? attacks = null, IReadOnlyList<string>? retreatCostList = null)
    {
        var card = new Card
        {
            Id = id,
            SetId = setId,
            Name = name,
            Supertype = supertype,
            Subtypes = new[] { subtype },
            Types = types,
            Hp = hp?.ToString(),
            HpValue = hp,
            Rarity = rarity,
            ConvertedRetreatCost = retreatCost,
            RetreatCost = retreatCostList ?? Array.Empty<string>(),
            Abilities = abilities ?? Array.Empty<Ability>(),
            Attacks = attacks ?? Array.Empty<Attack>(),
            Artist = artist,
            Number = id,
            Weaknesses = weaknessType is null ? Array.Empty<TypeEffect>() : new[] { new TypeEffect(weaknessType, "×2") },
            Resistances = resistanceType is null ? Array.Empty<TypeEffect>() : new[] { new TypeEffect(resistanceType, "-30") },
        };
        db.Cards.Add(card);
        db.CardVariants.Add(new CardVariant { Id = Guid.NewGuid(), CardId = id, VariantTypeId = normalVariantTypeId });

        db.CardSubtypes.Add(new CardSubtype { CardId = id, Subtype = subtype });
        foreach (var type in types)
        {
            db.CardTypes.Add(new CardType { CardId = id, Type = type });
        }
        if (weaknessType is not null)
        {
            db.CardWeaknessTypes.Add(new CardWeaknessType { CardId = id, Type = weaknessType });
        }
        if (resistanceType is not null)
        {
            db.CardResistanceTypes.Add(new CardResistanceType { CardId = id, Type = resistanceType });
        }
    }
}
