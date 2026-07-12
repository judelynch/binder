using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Cards;

namespace PokeBinder.Infrastructure.Cards;

public class CardSubtypeConfiguration : IEntityTypeConfiguration<CardSubtype>
{
    public void Configure(EntityTypeBuilder<CardSubtype> builder)
    {
        builder.HasKey(s => new { s.CardId, s.Subtype });
        builder.Property(s => s.CardId).HasMaxLength(50);
        builder.Property(s => s.Subtype).HasMaxLength(30);
        builder.HasIndex(s => s.Subtype);
    }
}
