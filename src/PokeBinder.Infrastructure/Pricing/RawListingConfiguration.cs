using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Pricing;

namespace PokeBinder.Infrastructure.Pricing;

public class RawListingConfiguration : IEntityTypeConfiguration<RawListing>
{
    public void Configure(EntityTypeBuilder<RawListing> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Query).HasMaxLength(500);
        builder.Property(l => l.ListingId).HasMaxLength(200).IsRequired();
        builder.Property(l => l.SourceProvider).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Title).HasMaxLength(500);
        builder.Property(l => l.ItemPriceGbp).HasColumnType("decimal(10,2)");
        builder.Property(l => l.PostagePriceGbp).HasColumnType("decimal(10,2)");
        builder.Property(l => l.ListingFormat).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.ThumbnailUrl).HasMaxLength(1000);

        builder.HasIndex(l => new { l.ListingId, l.SourceProvider }).IsUnique();
        builder.HasIndex(l => l.CardVariantId);
    }
}
