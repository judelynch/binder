using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Cards;

namespace PokeBinder.Infrastructure.Cards;

public class VariantTypeConfiguration : IEntityTypeConfiguration<VariantType>
{
    public void Configure(EntityTypeBuilder<VariantType> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(v => v.Name).IsUnique();
    }
}
