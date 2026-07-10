using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Cards;

namespace PokeBinder.Infrastructure.Cards;

public class CardPokedexNumberConfiguration : IEntityTypeConfiguration<CardPokedexNumber>
{
    public void Configure(EntityTypeBuilder<CardPokedexNumber> builder)
    {
        builder.HasKey(p => new { p.CardId, p.Number });
        builder.Property(p => p.CardId).HasMaxLength(50);
        builder.HasIndex(p => p.Number);
    }
}
