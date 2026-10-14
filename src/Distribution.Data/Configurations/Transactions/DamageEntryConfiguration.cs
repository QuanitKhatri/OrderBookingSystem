using Distribution.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Distribution.Data.Configurations.Transactions;

public class DamageEntryConfiguration : IEntityTypeConfiguration<DamageEntry>
{
    public void Configure(EntityTypeBuilder<DamageEntry> builder)
    {
        builder.ToTable("DamageEntries");
        builder.HasIndex(x => x.RefNo).IsUnique();
        builder.Property(x => x.RefNo).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.Property(x => x.Qty).HasColumnType("decimal(18,3)");
        builder.Property(x => x.CostRate).HasColumnType("decimal(18,2)");
        builder.Property(x => x.LossAmount).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DamageReason)
            .WithMany()
            .HasForeignKey(x => x.DamageReasonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
