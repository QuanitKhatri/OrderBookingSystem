using Distribution.Domain.Masters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Distribution.Data.Configurations.Masters;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Barcode);
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Barcode).HasMaxLength(50);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.Property(x => x.PcsPerUnit).HasColumnType("decimal(18,3)");
        builder.Property(x => x.PurchaseRate).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SaleRate).HasColumnType("decimal(18,2)");
        builder.Property(x => x.AvgCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MaxDiscountPct).HasColumnType("decimal(5,2)");

        builder.HasOne(x => x.Company)
            .WithMany(c => c.Products)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
