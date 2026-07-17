using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Pricing;

namespace PokeBinder.Infrastructure.Pricing;

public class PricePointConfiguration : IEntityTypeConfiguration<PricePoint>
{
    public void Configure(EntityTypeBuilder<PricePoint> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.GradedStatus).HasConversion<string>().HasMaxLength(10);
        builder.Property(p => p.Grader).HasMaxLength(20);
        builder.Property(p => p.Grade).HasColumnType("decimal(4,1)");
        builder.Property(p => p.Condition).HasConversion<string>().HasMaxLength(15);
        builder.Property(p => p.ItemOnlyMedianGbp).HasColumnType("decimal(10,2)");
        builder.Property(p => p.DeliveredMedianGbp).HasColumnType("decimal(10,2)");
        builder.Property(p => p.MinGbp).HasColumnType("decimal(10,2)");
        builder.Property(p => p.MaxGbp).HasColumnType("decimal(10,2)");
        builder.Property(p => p.QuarantinedReason).HasMaxLength(200);

        // A combined 6-column unique index over nullable Grader/Grade/Condition doesn't express the
        // real constraint: EF's default filtered-index behavior requires ALL nullable columns
        // non-null simultaneously, which never happens for either bucket type (Raw buckets have
        // null Grader/Grade; Graded buckets have null Condition) - it would silently enforce
        // nothing. Two indexes, one per bucket type's actual natural key, instead.
        builder.HasIndex(p => new { p.CardVariantId, p.Condition, p.WindowDays })
            .IsUnique()
            .HasFilter("[GradedStatus] = 'Raw'")
            .HasDatabaseName("IX_PricePoints_Raw_Bucket");

        builder.HasIndex(p => new { p.CardVariantId, p.Grader, p.Grade, p.WindowDays })
            .IsUnique()
            .HasFilter("[GradedStatus] = 'Graded'")
            .HasDatabaseName("IX_PricePoints_Graded_Bucket");
    }
}
