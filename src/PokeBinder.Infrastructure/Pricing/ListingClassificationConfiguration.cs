using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Pricing;

namespace PokeBinder.Infrastructure.Pricing;

public class ListingClassificationConfiguration : IEntityTypeConfiguration<ListingClassification>
{
    public void Configure(EntityTypeBuilder<ListingClassification> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.GradedStatus).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.Grader).HasMaxLength(20);
        builder.Property(c => c.Grade).HasColumnType("decimal(4,1)");
        builder.Property(c => c.RawCondition).HasConversion<string>().HasMaxLength(15);
        builder.Property(c => c.VariantMatch).HasConversion<string>().HasMaxLength(15);
        builder.Property(c => c.Language).HasMaxLength(50);
        builder.Property(c => c.KillReason).HasMaxLength(200);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(c => c.RawListingId).IsUnique();
        builder.HasIndex(c => c.ResolvedCardVariantId);
        builder.HasIndex(c => c.Status);

        builder.HasOne(c => c.RawListing)
            .WithMany()
            .HasForeignKey(c => c.RawListingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
