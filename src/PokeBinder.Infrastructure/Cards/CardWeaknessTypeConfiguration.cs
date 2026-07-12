using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Cards;

namespace PokeBinder.Infrastructure.Cards;

public class CardWeaknessTypeConfiguration : IEntityTypeConfiguration<CardWeaknessType>
{
    public void Configure(EntityTypeBuilder<CardWeaknessType> builder)
    {
        builder.HasKey(w => new { w.CardId, w.Type });
        builder.Property(w => w.CardId).HasMaxLength(50);
        builder.Property(w => w.Type).HasMaxLength(30);
        builder.HasIndex(w => w.Type);
    }
}
