using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Cards;

namespace PokeBinder.Infrastructure.Cards;

public class CardVariantConfiguration : IEntityTypeConfiguration<CardVariant>
{
    public void Configure(EntityTypeBuilder<CardVariant> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.CardId).HasMaxLength(50);

        builder.HasIndex(v => new { v.CardId, v.VariantTypeId }).IsUnique();

        builder.HasOne(v => v.VariantType)
            .WithMany()
            .HasForeignKey(v => v.VariantTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
