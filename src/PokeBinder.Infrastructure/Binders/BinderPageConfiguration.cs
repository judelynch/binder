using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Binders;

namespace PokeBinder.Infrastructure.Binders;

public class BinderPageConfiguration : IEntityTypeConfiguration<BinderPage>
{
    public void Configure(EntityTypeBuilder<BinderPage> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.BinderId, p.PageNumber }).IsUnique();

        builder.HasMany(p => p.Slots)
            .WithOne(s => s.Page)
            .HasForeignKey(s => s.PageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
