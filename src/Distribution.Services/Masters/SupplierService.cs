using Distribution.Data;
using Distribution.Domain.Masters;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Masters;

public class SupplierService(DistributionDbContext db) : ISupplierService
{
    public async Task<List<SupplierListItem>> SearchAsync(string? query, CancellationToken ct = default)
    {
        var q = db.Suppliers.Include(s => s.Company).Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(s => EF.Functions.ILike(s.Name, $"%{term}%") || EF.Functions.ILike(s.Code, $"%{term}%"));
        }

        return await q.OrderBy(s => s.Name)
            .Select(s => new SupplierListItem(s.Id, s.Code, s.Name, s.Company.Name, s.OpeningBalance, s.IsActive))
            .ToListAsync(ct);
    }

    public async Task<SupplierEditModel?> GetAsync(int id, CancellationToken ct = default)
    {
        return await db.Suppliers.Where(s => s.Id == id)
            .Select(s => new SupplierEditModel(s.Id, s.Code, s.Name, s.CompanyId, s.ContactPerson, s.Phone, s.Address, s.OpeningBalance))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CreateAsync(SupplierEditModel model, string performedBy, CancellationToken ct = default)
    {
        var entity = new Supplier
        {
            Code = model.Code.Trim(),
            Name = model.Name.Trim(),
            CompanyId = model.CompanyId,
            ContactPerson = model.ContactPerson,
            Phone = model.Phone,
            Address = model.Address,
            OpeningBalance = model.OpeningBalance,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };
        db.Suppliers.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(SupplierEditModel model, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == model.Id, ct)
            ?? throw new InvalidOperationException("Supplier not found.");

        entity.Code = model.Code.Trim();
        entity.Name = model.Name.Trim();
        entity.CompanyId = model.CompanyId;
        entity.ContactPerson = model.ContactPerson;
        entity.Phone = model.Phone;
        entity.Address = model.Address;
        entity.OpeningBalance = model.OpeningBalance;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new InvalidOperationException("Supplier not found.");

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }
}
