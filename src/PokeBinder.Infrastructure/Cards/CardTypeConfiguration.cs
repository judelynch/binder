using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Cards;

namespace PokeBinder.Infrastructure.Cards;

public class CardTypeConfiguration : IEntityTypeConfiguration<CardType>
{
    public void Configure(EntityTypeBuilder<CardType> builder)
    {
        builder.HasKey(t => new { t.CardId, t.Type });
        builder.Property(t => t.CardId).HasMaxLength(50);
        builder.Property(t => t.Type).HasMaxLength(30);
        builder.HasIndex(t => t.Type);
    }
}
