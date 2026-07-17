using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Pricing;

namespace PokeBinder.Infrastructure.Pricing;

public class ScrapeRunConfiguration : IEntityTypeConfiguration<ScrapeRun>
{
    public void Configure(EntityTypeBuilder<ScrapeRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.TriggeredBy).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.TriggeredByUserId).HasMaxLength(450);
        builder.Property(r => r.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(r => r.StartedAt);
        builder.HasIndex(r => r.Status);
    }
}
