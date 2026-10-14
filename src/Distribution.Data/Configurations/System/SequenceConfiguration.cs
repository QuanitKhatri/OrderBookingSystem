using Distribution.Domain.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Distribution.Data.Configurations.System;

public class SequenceConfiguration : IEntityTypeConfiguration<Sequence>
{
    public void Configure(EntityTypeBuilder<Sequence> builder)
    {
        builder.ToTable("Sequences");
        builder.HasKey(x => x.Name);
        builder.Property(x => x.Name).HasMaxLength(50);
    }
}
