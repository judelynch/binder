using PokeBinder.Core.Cards;
using PokeBinder.Infrastructure;

namespace PokeBinder.Tests;

/// <summary>Seeds a handful of minimal Card/CardVariant rows directly, bypassing the real importer, for tests that just need valid CardVariantIds to assign into binder slots.</summary>
public static class CardFixture
{
    public static async Task<List<Guid>> SeedCardVariantsAsync(PokeBinderDbContext db, int count)
    {
        var set = new Set
        {
            Id = "fixture-set",
            Name = "Fixture Set",
            Series = "Fixture",
            PrintedTotal = count,
            Total = count,
            ReleaseDate = new DateOnly(2020, 1, 1),
            UpdatedAt = DateTime.UtcNow
        };
        db.Sets.Add(set);

        var normalVariantType = new VariantType { Id = Guid.NewGuid(), Name = "Normal" };
        db.VariantTypes.Add(normalVariantType);

        var variantIds = new List<Guid>();

        for (var i = 1; i <= count; i++)
        {
            var card = new Card
            {
                Id = $"fixture-set-{i}",
                SetId = set.Id,
                Name = $"Fixture Card {i}",
                Supertype = "Pokémon",
                Number = i.ToString(),
                NumberSortGroup = 0,
                NumberSortValue = i
            };
            db.Cards.Add(card);

            var variant = new CardVariant { Id = Guid.NewGuid(), CardId = card.Id, VariantTypeId = normalVariantType.Id };
            db.CardVariants.Add(variant);
            variantIds.Add(variant.Id);
        }

        await db.SaveChangesAsync();
        return variantIds;
    }
}
