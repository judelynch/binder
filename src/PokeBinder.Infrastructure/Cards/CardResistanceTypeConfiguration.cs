using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Cards;

namespace PokeBinder.Infrastructure.Cards;

public class CardResistanceTypeConfiguration : IEntityTypeConfiguration<CardResistanceType>
{
    public void Configure(EntityTypeBuilder<CardResistanceType> builder)
    {
        builder.HasKey(r => new { r.CardId, r.Type });
        builder.Property(r => r.CardId).HasMaxLength(50);
        builder.Property(r => r.Type).HasMaxLength(30);
        builder.HasIndex(r => r.Type);
    }
}
