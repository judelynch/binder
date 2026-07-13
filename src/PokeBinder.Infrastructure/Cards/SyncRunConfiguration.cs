using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Cards;

namespace PokeBinder.Infrastructure.Cards;

public class SyncRunConfiguration : IEntityTypeConfiguration<SyncRun>
{
    public void Configure(EntityTypeBuilder<SyncRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RunByUserId).HasMaxLength(450);
        builder.Property(r => r.RunByEmail).HasMaxLength(256);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.ErrorMessage).HasMaxLength(2000);

        builder.Property(r => r.ChangedFieldCounts)
            .HasConversion(new JsonValueConverter<IReadOnlyList<SyncFieldChange>>())
            .HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new JsonListValueComparer<SyncFieldChange>());

        builder.Property(r => r.RemainingManualConflicts)
            .HasConversion(new JsonValueConverter<IReadOnlyList<SyncManualConflict>>())
            .HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new JsonListValueComparer<SyncManualConflict>());

        builder.HasIndex(r => r.StartedAt);
        builder.HasIndex(r => r.Status);
    }
}
