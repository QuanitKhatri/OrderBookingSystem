using Distribution.Data;
using Distribution.Domain.Masters;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Masters;

public class AreaService(DistributionDbContext db) : IAreaService
{
    public async Task<List<(int Id, string Name)>> GetActiveLookupAsync(CancellationToken ct = default)
    {
        return await db.Areas.Where(a => a.IsActive && !a.IsDeleted)
            .OrderBy(a => a.Name)
            .Select(a => new ValueTuple<int, string>(a.Id, a.Name))
            .ToListAsync(ct);
    }

    public async Task<int> CreateAsync(string name, string performedBy, CancellationToken ct = default)
    {
        var entity = new Area
        {
            Name = name.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };
        db.Areas.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
