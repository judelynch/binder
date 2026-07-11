using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Binders;

namespace PokeBinder.Infrastructure.Binders;

public class BinderSlotConfiguration : IEntityTypeConfiguration<BinderSlot>
{
    public void Configure(EntityTypeBuilder<BinderSlot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.CardVariantId);
        builder.Property(s => s.Condition).HasConversion<string>().HasMaxLength(10);

        builder.HasIndex(s => new { s.PageId, s.Position }).IsUnique();
        builder.HasIndex(s => s.CardVariantId);
        builder.HasIndex(s => s.OverlayTagId);

        builder.HasOne(s => s.CardVariant)
            .WithMany()
            .HasForeignKey(s => s.CardVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.OverlayTag)
            .WithMany()
            .HasForeignKey(s => s.OverlayTagId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
