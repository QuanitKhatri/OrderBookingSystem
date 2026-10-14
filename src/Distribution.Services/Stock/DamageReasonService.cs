using Distribution.Data;
using Distribution.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Stock;

public class DamageReasonService(DistributionDbContext db) : IDamageReasonService
{
    public async Task<List<(int Id, string Name)>> GetActiveLookupAsync(CancellationToken ct = default)
    {
        return await db.DamageReasons.Where(r => r.IsActive && !r.IsDeleted)
            .OrderBy(r => r.Name)
            .Select(r => new ValueTuple<int, string>(r.Id, r.Name))
            .ToListAsync(ct);
    }

    public async Task<int> CreateAsync(string name, string performedBy, CancellationToken ct = default)
    {
        var entity = new DamageReason
        {
            Name = name.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };
        db.DamageReasons.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
