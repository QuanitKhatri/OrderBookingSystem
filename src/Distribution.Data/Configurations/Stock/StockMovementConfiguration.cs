using Distribution.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Distribution.Data.Configurations.Stock;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasIndex(x => new { x.ProductId, x.WarehouseId, x.Date });
        builder.HasIndex(x => new { x.RefType, x.RefId });
        builder.Property(x => x.RefType).HasMaxLength(30).IsRequired();

        builder.Property(x => x.QtyIn).HasColumnType("decimal(18,3)");
        builder.Property(x => x.QtyOut).HasColumnType("decimal(18,3)");
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
