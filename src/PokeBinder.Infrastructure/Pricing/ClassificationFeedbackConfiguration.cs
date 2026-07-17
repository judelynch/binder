using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Pricing;

namespace PokeBinder.Infrastructure.Pricing;

public class ClassificationFeedbackConfiguration : IEntityTypeConfiguration<ClassificationFeedback>
{
    public void Configure(EntityTypeBuilder<ClassificationFeedback> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.OriginalGuessJson).HasColumnType("nvarchar(max)");
        builder.Property(f => f.CorrectedValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(f => f.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Reason).HasMaxLength(500);
        builder.Property(f => f.ReviewedByUserId).HasMaxLength(450);

        builder.HasIndex(f => f.ListingClassificationId);

        builder.HasOne(f => f.ListingClassification)
            .WithMany()
            .HasForeignKey(f => f.ListingClassificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
