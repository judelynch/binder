using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Cards;

namespace PokeBinder.Infrastructure.Cards;

public class SetConfiguration : IEntityTypeConfiguration<Set>
{
    public void Configure(EntityTypeBuilder<Set> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasMaxLength(50);
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Series).HasMaxLength(200);
        builder.Property(s => s.PtcgoCode).HasMaxLength(20);

        builder.Property(s => s.Legalities)
            .HasConversion(new JsonValueConverter<IReadOnlyDictionary<string, string>>())
            .HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new JsonDictionaryValueComparer<string, string>());

        builder.HasIndex(s => s.ReleaseDate);

        builder.HasMany(s => s.Cards)
            .WithOne(c => c.Set)
            .HasForeignKey(c => c.SetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
