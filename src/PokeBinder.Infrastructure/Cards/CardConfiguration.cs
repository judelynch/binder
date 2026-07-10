using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Cards;

namespace PokeBinder.Infrastructure.Cards;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasMaxLength(50);
        builder.Property(c => c.SetId).HasMaxLength(50);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Supertype).HasMaxLength(50);
        builder.Property(c => c.Level).HasMaxLength(20);
        builder.Property(c => c.Hp).HasMaxLength(20);
        builder.Property(c => c.EvolvesFrom).HasMaxLength(200);
        builder.Property(c => c.Number).HasMaxLength(20).IsRequired();
        builder.Property(c => c.NumberSortPrefix).HasMaxLength(10);
        builder.Property(c => c.NumberSortSuffix).HasMaxLength(5);
        builder.Property(c => c.Artist).HasMaxLength(200);
        builder.Property(c => c.Rarity).HasMaxLength(100);
        builder.Property(c => c.RegulationMark).HasMaxLength(10);

        builder.Property(c => c.Subtypes).HasConversion(new JsonValueConverter<IReadOnlyList<string>>()).HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new JsonListValueComparer<string>());
        builder.Property(c => c.Types).HasConversion(new JsonValueConverter<IReadOnlyList<string>>()).HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new JsonListValueComparer<string>());
        builder.Property(c => c.RetreatCost).HasConversion(new JsonValueConverter<IReadOnlyList<string>>()).HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new JsonListValueComparer<string>());
        builder.Property(c => c.Abilities).HasConversion(new JsonValueConverter<IReadOnlyList<Ability>>()).HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new JsonListValueComparer<Ability>());
        builder.Property(c => c.Attacks).HasConversion(new JsonValueConverter<IReadOnlyList<Attack>>()).HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new JsonListValueComparer<Attack>());
        builder.Property(c => c.Weaknesses).HasConversion(new JsonValueConverter<IReadOnlyList<TypeEffect>>()).HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new JsonListValueComparer<TypeEffect>());
        builder.Property(c => c.Resistances).HasConversion(new JsonValueConverter<IReadOnlyList<TypeEffect>>()).HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new JsonListValueComparer<TypeEffect>());
        builder.Property(c => c.Legalities).HasConversion(new JsonValueConverter<IReadOnlyDictionary<string, string>>()).HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new JsonDictionaryValueComparer<string, string>());

        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.SetId);
        builder.HasIndex(c => c.Rarity);
        builder.HasIndex(c => c.Supertype);
        builder.HasIndex(c => c.Artist);
        builder.HasIndex(c => c.RegulationMark);
        builder.HasIndex(c => new { c.SetId, c.NumberSortGroup, c.NumberSortPrefix, c.NumberSortValue, c.NumberSortSuffix })
            .HasDatabaseName("IX_Card_Set_NumberSortKey");

        builder.HasMany(c => c.PokedexNumbers)
            .WithOne(p => p.Card)
            .HasForeignKey(p => p.CardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Variants)
            .WithOne(v => v.Card)
            .HasForeignKey(v => v.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
