using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Collection;

namespace PokeBinder.Infrastructure.Collection;

public class CardOwnershipConfiguration : IEntityTypeConfiguration<CardOwnership>
{
    public void Configure(EntityTypeBuilder<CardOwnership> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.UserId).HasMaxLength(450).IsRequired();
        builder.Property(o => o.Condition).HasConversion<string>().HasMaxLength(10);

        builder.HasIndex(o => new { o.UserId, o.CardVariantId }).IsUnique();

        // Cascade, unlike BinderSlotConfiguration's Restrict on the same FK: a BinderSlot losing
        // its CardVariantId would silently corrupt a binder layout, so that one is protected.
        // A CardOwnership row is just a fact about a variant ("the user owns this") - if the
        // variant is ever removed during a catalog resync, the ownership fact should go with it.
        builder.HasOne(o => o.CardVariant)
            .WithMany()
            .HasForeignKey(o => o.CardVariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
