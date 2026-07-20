using PokeBinder.Core.Cards;
using PokeBinder.Infrastructure;

namespace PokeBinder.Tests;

/// <summary>Seeds a small, deliberately cross-set/cross-name/cross-rarity catalog for exercising all four suggestion theme categories end-to-end.</summary>
public static class SuggestionCardFixture
{
    public const string GengarSet1 = "gengar-1";
    public const string HaunterSet1 = "haunter-2";
    public const string OddishSet1 = "oddish-3";
    public const string GengarSet2 = "gengar-4";
    public const string TangelaSet2 = "tangela-5";
    public const string GengarSet3 = "gengar-6"; // not placed in any test - the "next release" candidate
    public const string TrainerBill = "trainer-bill";
    public const string TrainerOak = "trainer-oak";
    public const string TrainerMarnie = "trainer-marnie"; // not placed - the RaritySupertype candidate

    public static async Task<Dictionary<string, Guid>> SeedAsync(PokeBinderDbContext db)
    {
        var set1 = new Set { Id = "sug-set1", Name = "Suggestion Set One", Series = "Suggestion Series", PrintedTotal = 3, Total = 3, ReleaseDate = new DateOnly(2000, 1, 1), UpdatedAt = DateTime.UtcNow };
        var set2 = new Set { Id = "sug-set2", Name = "Suggestion Set Two", Series = "Suggestion Series", PrintedTotal = 2, Total = 2, ReleaseDate = new DateOnly(2001, 1, 1), UpdatedAt = DateTime.UtcNow };
        var set3 = new Set { Id = "sug-set3", Name = "Suggestion Set Three", Series = "Suggestion Series", PrintedTotal = 1, Total = 1, ReleaseDate = new DateOnly(2002, 1, 1), UpdatedAt = DateTime.UtcNow };
        // Each Trainer gets its own isolated set - one card each, so no two of them ever tie the Set
        // category against RaritySupertype (which is exactly what the RaritySupertype-theme test needs).
        var set4 = new Set { Id = "sug-set4", Name = "Suggestion Set Four (Trainer)", Series = "Suggestion Series", PrintedTotal = 1, Total = 1, ReleaseDate = new DateOnly(2003, 1, 1), UpdatedAt = DateTime.UtcNow };
        var set5 = new Set { Id = "sug-set5", Name = "Suggestion Set Five (Trainer)", Series = "Suggestion Series", PrintedTotal = 1, Total = 1, ReleaseDate = new DateOnly(2004, 1, 1), UpdatedAt = DateTime.UtcNow };
        var set6 = new Set { Id = "sug-set6", Name = "Suggestion Set Six (Trainer)", Series = "Suggestion Series", PrintedTotal = 1, Total = 1, ReleaseDate = new DateOnly(2005, 1, 1), UpdatedAt = DateTime.UtcNow };
        db.Sets.AddRange(set1, set2, set3, set4, set5, set6);

        var normalVariantType = new VariantType { Id = Guid.NewGuid(), Name = "Normal" };
        db.VariantTypes.Add(normalVariantType);

        var variantIds = new Dictionary<string, Guid>();

        void AddCard(string id, string name, string setId, string number, string supertype, string rarity, string[] types)
        {
            var card = new Card
            {
                Id = id,
                SetId = setId,
                Name = name,
                Supertype = supertype,
                Number = number,
                NumberSortGroup = 0,
                NumberSortValue = int.Parse(number),
                Rarity = rarity,
                Types = types,
            };
            db.Cards.Add(card);
            foreach (var type in types)
            {
                db.CardTypes.Add(new CardType { CardId = id, Type = type });
            }

            var variant = new CardVariant { Id = Guid.NewGuid(), CardId = id, VariantTypeId = normalVariantType.Id };
            db.CardVariants.Add(variant);
            variantIds[id] = variant.Id;
        }

        AddCard(GengarSet1, "Gengar", set1.Id, "1", "Pokémon", "Rare Holo", new[] { "Ghost" });
        AddCard(HaunterSet1, "Haunter", set1.Id, "2", "Pokémon", "Rare Holo", new[] { "Ghost" });
        AddCard(OddishSet1, "Oddish", set1.Id, "3", "Pokémon", "Illustration Rare", new[] { "Grass" });
        AddCard(GengarSet2, "Gengar", set2.Id, "1", "Pokémon", "Rare Holo", new[] { "Ghost" });
        AddCard(TangelaSet2, "Tangela", set2.Id, "2", "Pokémon", "Illustration Rare", new[] { "Grass" });
        AddCard(GengarSet3, "Gengar", set3.Id, "1", "Pokémon", "Rare Holo", new[] { "Ghost" });
        AddCard(TrainerBill, "Bill", set4.Id, "1", "Trainer", "Ultra Rare", Array.Empty<string>());
        AddCard(TrainerOak, "Oak", set5.Id, "1", "Trainer", "Ultra Rare", Array.Empty<string>());
        AddCard(TrainerMarnie, "Marnie", set6.Id, "1", "Trainer", "Ultra Rare", Array.Empty<string>());

        await db.SaveChangesAsync();
        return variantIds;
    }
}
