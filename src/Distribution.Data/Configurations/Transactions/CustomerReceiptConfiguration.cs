using Distribution.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Distribution.Data.Configurations.Transactions;

public class CustomerReceiptConfiguration : IEntityTypeConfiguration<CustomerReceipt>
{
    public void Configure(EntityTypeBuilder<CustomerReceipt> builder)
    {
        builder.ToTable("CustomerReceipts");
        builder.HasIndex(x => x.ReceiptNo).IsUnique();
        builder.Property(x => x.ReceiptNo).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Sale)
            .WithMany()
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
