using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokeBinder.Core.Cards;

namespace PokeBinder.Infrastructure.Cards;

public class CardEditAuditConfiguration : IEntityTypeConfiguration<CardEditAudit>
{
    public void Configure(EntityTypeBuilder<CardEditAudit> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.CardId).HasMaxLength(50);
        builder.Property(a => a.EditedByUserId).HasMaxLength(450);
        builder.Property(a => a.EditedByEmail).HasMaxLength(256);
        builder.Property(a => a.Note).HasMaxLength(1000).IsRequired();

        builder.Property(a => a.ChangedFields)
            .HasConversion(new JsonValueConverter<IReadOnlyList<string>>())
            .HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(new JsonListValueComparer<string>());

        builder.HasIndex(a => a.CardId);
        builder.HasIndex(a => a.EditedAt);

        builder.HasOne(a => a.Card)
            .WithMany()
            .HasForeignKey(a => a.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
