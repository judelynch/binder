using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Binders;

namespace PokeBinder.Infrastructure.Binders;

public class OverlayTagConfiguration : IEntityTypeConfiguration<OverlayTag>
{
    public void Configure(EntityTypeBuilder<OverlayTag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.OwnerId).HasMaxLength(450).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.ColourHex).HasMaxLength(7).IsRequired();

        builder.HasIndex(t => new { t.OwnerId, t.Name }).IsUnique();
    }
}
