using PokeBinder.Core.Cards;
using PokeBinder.Infrastructure;

namespace PokeBinder.Tests;

/// <summary>Seeds a small, deliberately cross-set catalog for exercising all three suggestion rules end-to-end.</summary>
public static class SuggestionCardFixture
{
    public const string GengarSet1 = "gengar-1";
    public const string HaunterSet1 = "haunter-2";
    public const string OddishSet1 = "oddish-3";
    public const string GengarSet2 = "gengar-4";
    public const string TangelaSet2 = "tangela-5";

    public static async Task<Dictionary<string, Guid>> SeedAsync(PokeBinderDbContext db)
    {
        var set1 = new Set { Id = "sug-set1", Name = "Suggestion Set One", Series = "Suggestion Series", PrintedTotal = 3, Total = 3, ReleaseDate = new DateOnly(2000, 1, 1), UpdatedAt = DateTime.UtcNow };
        var set2 = new Set { Id = "sug-set2", Name = "Suggestion Set Two", Series = "Suggestion Series", PrintedTotal = 2, Total = 2, ReleaseDate = new DateOnly(2001, 1, 1), UpdatedAt = DateTime.UtcNow };
        db.Sets.AddRange(set1, set2);

        var normalVariantType = new VariantType { Id = Guid.NewGuid(), Name = "Normal" };
        db.VariantTypes.Add(normalVariantType);

        var variantIds = new Dictionary<string, Guid>();

        void AddCard(string id, string name, string setId, string number, string rarity, string[] types)
        {
            var card = new Card
            {
                Id = id,
                SetId = setId,
                Name = name,
                Supertype = "Pokémon",
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

        AddCard(GengarSet1, "Gengar", set1.Id, "1", "Rare Holo", new[] { "Ghost" });
        AddCard(HaunterSet1, "Haunter", set1.Id, "2", "Rare Holo", new[] { "Ghost" });
        AddCard(OddishSet1, "Oddish", set1.Id, "3", "Illustration Rare", new[] { "Grass" });
        AddCard(GengarSet2, "Gengar", set2.Id, "1", "Rare Holo", new[] { "Ghost" });
        AddCard(TangelaSet2, "Tangela", set2.Id, "2", "Illustration Rare", new[] { "Grass" });

        await db.SaveChangesAsync();
        return variantIds;
    }
}
