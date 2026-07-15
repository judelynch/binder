using PokeBinder.Core.Cards;
using PokeBinder.Infrastructure;

namespace PokeBinder.Tests;

/// <summary>
/// Seeds a small set purpose-built to exercise the "Stamp variants don't count toward
/// completion" rule, including the case-insensitivity requirement and the vacuous-completion
/// edge case (a card whose only variant is a Stamp variant).
/// </summary>
public static class SetCompletionFixture
{
    public static async Task<SetCompletionFixtureIds> SeedAsync(PokeBinderDbContext db)
    {
        var set = new Set
        {
            Id = "completion-set",
            Name = "Completion Test Set",
            Series = "Completion Series",
            PrintedTotal = 4,
            Total = 4,
            ReleaseDate = new DateOnly(2022, 3, 1),
            UpdatedAt = DateTime.UtcNow,
        };
        db.Sets.Add(set);

        var normal = new VariantType { Id = Guid.NewGuid(), Name = "Normal" };
        var reverseHolo = new VariantType { Id = Guid.NewGuid(), Name = "Reverse Holo" };
        var promoStamp = new VariantType { Id = Guid.NewGuid(), Name = "Promo Stamp" };
        var allCapsStamp = new VariantType { Id = Guid.NewGuid(), Name = "STAMP" };
        db.VariantTypes.AddRange(normal, reverseHolo, promoStamp, allCapsStamp);

        // Card 1: Normal + Reverse Holo (required) + Promo Stamp (excluded) - the core rule test.
        db.Cards.Add(new Card { Id = "completion-1", SetId = set.Id, Name = "Card One", Supertype = "Pokémon", Number = "1" });
        var card1NormalId = Guid.NewGuid();
        var card1ReverseHoloId = Guid.NewGuid();
        var card1PromoStampId = Guid.NewGuid();
        db.CardVariants.Add(new CardVariant { Id = card1NormalId, CardId = "completion-1", VariantTypeId = normal.Id });
        db.CardVariants.Add(new CardVariant { Id = card1ReverseHoloId, CardId = "completion-1", VariantTypeId = reverseHolo.Id });
        db.CardVariants.Add(new CardVariant { Id = card1PromoStampId, CardId = "completion-1", VariantTypeId = promoStamp.Id });

        // Card 2: only a Promo Stamp variant - vacuously complete with zero ownership.
        db.Cards.Add(new Card { Id = "completion-2", SetId = set.Id, Name = "Card Two", Supertype = "Pokémon", Number = "2" });
        var card2PromoStampId = Guid.NewGuid();
        db.CardVariants.Add(new CardVariant { Id = card2PromoStampId, CardId = "completion-2", VariantTypeId = promoStamp.Id });

        // Card 3: only Normal - a plain "not owned yet" card, to prove OwnedCount doesn't over-count.
        db.Cards.Add(new Card { Id = "completion-3", SetId = set.Id, Name = "Card Three", Supertype = "Pokémon", Number = "3" });
        var card3NormalId = Guid.NewGuid();
        db.CardVariants.Add(new CardVariant { Id = card3NormalId, CardId = "completion-3", VariantTypeId = normal.Id });

        // Card 4: only an all-caps "STAMP" variant - proves the exclusion is case-insensitive,
        // not just a literal match on "Stamp". If case-insensitivity were broken, this variant
        // would wrongly count as required, and the card would show unowned/incomplete instead
        // of vacuously complete.
        db.Cards.Add(new Card { Id = "completion-4", SetId = set.Id, Name = "Card Four", Supertype = "Pokémon", Number = "4" });
        var card4StampId = Guid.NewGuid();
        db.CardVariants.Add(new CardVariant { Id = card4StampId, CardId = "completion-4", VariantTypeId = allCapsStamp.Id });

        await db.SaveChangesAsync();

        return new SetCompletionFixtureIds(
            set.Id, card1NormalId, card1ReverseHoloId, card1PromoStampId, card2PromoStampId, card3NormalId, card4StampId);
    }
}

public record SetCompletionFixtureIds(
    string SetId,
    Guid Card1NormalVariantId,
    Guid Card1ReverseHoloVariantId,
    Guid Card1PromoStampVariantId,
    Guid Card2PromoStampVariantId,
    Guid Card3NormalVariantId,
    Guid Card4StampVariantId);
