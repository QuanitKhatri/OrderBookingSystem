using Distribution.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Distribution.Data.Configurations.Transactions;

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("PurchaseItems");

        builder.Property(x => x.Qty).HasColumnType("decimal(18,3)");
        builder.Property(x => x.Rate).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.AvgCostBefore).HasColumnType("decimal(18,2)");
        builder.Property(x => x.AvgCostAfter).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
