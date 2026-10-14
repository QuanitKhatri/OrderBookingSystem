using Distribution.Data;
using Distribution.Domain.Masters;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Masters;

public class CompanyService(DistributionDbContext db) : ICompanyService
{
    public async Task<List<CompanyListItem>> SearchAsync(string? query, CancellationToken ct = default)
    {
        var q = db.Companies.Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(c => EF.Functions.ILike(c.Name, $"%{term}%") || EF.Functions.ILike(c.Code, $"%{term}%"));
        }

        return await q.OrderBy(c => c.Name)
            .Select(c => new CompanyListItem(c.Id, c.Code, c.Name, c.ContactPerson, c.Phone, c.IsActive))
            .ToListAsync(ct);
    }

    public async Task<CompanyEditModel?> GetAsync(int id, CancellationToken ct = default)
    {
        return await db.Companies.Where(c => c.Id == id)
            .Select(c => new CompanyEditModel(c.Id, c.Code, c.Name, c.ContactPerson, c.Phone))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<(int Id, string Name)>> GetActiveLookupAsync(CancellationToken ct = default)
    {
        return await db.Companies.Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new ValueTuple<int, string>(c.Id, c.Name))
            .ToListAsync(ct);
    }

    public async Task<int> CreateAsync(CompanyEditModel model, string performedBy, CancellationToken ct = default)
    {
        var entity = new Company
        {
            Code = model.Code.Trim(),
            Name = model.Name.Trim(),
            ContactPerson = model.ContactPerson,
            Phone = model.Phone,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };
        db.Companies.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(CompanyEditModel model, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Companies.FirstOrDefaultAsync(c => c.Id == model.Id, ct)
            ?? throw new InvalidOperationException("Company not found.");

        entity.Code = model.Code.Trim();
        entity.Name = model.Name.Trim();
        entity.ContactPerson = model.ContactPerson;
        entity.Phone = model.Phone;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException("Company not found.");

        var hasProducts = await db.Products.AnyAsync(p => p.CompanyId == id && !p.IsDeleted, ct);
        if (hasProducts)
        {
            throw new InvalidOperationException("This company has active products linked to it and cannot be deleted. Deactivate or reassign those products first.");
        }

        var hasSuppliers = await db.Suppliers.AnyAsync(s => s.CompanyId == id && !s.IsDeleted, ct);
        if (hasSuppliers)
        {
            throw new InvalidOperationException("This company has active suppliers linked to it and cannot be deleted. Deactivate or reassign those suppliers first.");
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }
}
