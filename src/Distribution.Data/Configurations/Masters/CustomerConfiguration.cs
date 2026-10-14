using Distribution.Domain.Masters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Distribution.Data.Configurations.Masters;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ShopName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OwnerName).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.Property(x => x.OpeningBalance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DefaultDiscountPct).HasColumnType("decimal(5,2)");

        builder.HasOne(x => x.Area)
            .WithMany(a => a.Customers)
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
