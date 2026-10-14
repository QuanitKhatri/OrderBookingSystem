using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Distribution.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DistributionDbContext>
{
    public DistributionDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DISTRIBUTION_CONNECTION")
            ?? "Host=localhost;Database=distribution_dev;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<DistributionDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DistributionDbContext(optionsBuilder.Options);
    }
}
