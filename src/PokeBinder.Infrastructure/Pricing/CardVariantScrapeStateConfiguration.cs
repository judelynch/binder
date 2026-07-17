using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Pricing;

namespace PokeBinder.Infrastructure.Pricing;

public class CardVariantScrapeStateConfiguration : IEntityTypeConfiguration<CardVariantScrapeState>
{
    public void Configure(EntityTypeBuilder<CardVariantScrapeState> builder)
    {
        builder.HasKey(s => s.CardVariantId);
        builder.HasIndex(s => new { s.ScrapePriority, s.LastScrapedAt });

        builder.HasOne<Core.Cards.CardVariant>()
            .WithMany()
            .HasForeignKey(s => s.CardVariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
