using Distribution.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Distribution.Data.Configurations.Transactions;

public class DamageReasonConfiguration : IEntityTypeConfiguration<DamageReason>
{
    public void Configure(EntityTypeBuilder<DamageReason> builder)
    {
        builder.ToTable("DamageReasons");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);
    }
}
