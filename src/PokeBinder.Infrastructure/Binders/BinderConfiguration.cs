using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Binders;

namespace PokeBinder.Infrastructure.Binders;

public class BinderConfiguration : IEntityTypeConfiguration<Binder>
{
    public void Configure(EntityTypeBuilder<Binder> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.OwnerId).HasMaxLength(450).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.ColourHex).HasMaxLength(7).IsRequired();

        builder.HasIndex(b => b.OwnerId);
        builder.HasIndex(b => new { b.OwnerId, b.LastAccessedAt });

        builder.HasMany(b => b.Pages)
            .WithOne(p => p.Binder)
            .HasForeignKey(p => p.BinderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
