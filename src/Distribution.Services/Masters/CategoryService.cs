using Distribution.Data;
using Distribution.Domain.Masters;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Masters;

public class CategoryService(DistributionDbContext db) : ICategoryService
{
    public async Task<List<CategoryListItem>> SearchAsync(string? query, CancellationToken ct = default)
    {
        var q = db.Categories.Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(c => EF.Functions.ILike(c.Name, $"%{term}%"));
        }

        return await q.OrderBy(c => c.Name)
            .Select(c => new CategoryListItem(c.Id, c.Name, c.Description, c.IsActive))
            .ToListAsync(ct);
    }

    public async Task<CategoryEditModel?> GetAsync(int id, CancellationToken ct = default)
    {
        return await db.Categories.Where(c => c.Id == id)
            .Select(c => new CategoryEditModel(c.Id, c.Name, c.Description))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<(int Id, string Name)>> GetActiveLookupAsync(CancellationToken ct = default)
    {
        return await db.Categories.Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new ValueTuple<int, string>(c.Id, c.Name))
            .ToListAsync(ct);
    }

    public async Task<int> CreateAsync(CategoryEditModel model, string performedBy, CancellationToken ct = default)
    {
        var entity = new Category
        {
            Name = model.Name.Trim(),
            Description = model.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };
        db.Categories.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(CategoryEditModel model, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == model.Id, ct)
            ?? throw new InvalidOperationException("Category not found.");

        entity.Name = model.Name.Trim();
        entity.Description = model.Description;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException("Category not found.");

        var hasProducts = await db.Products.AnyAsync(p => p.CategoryId == id && !p.IsDeleted, ct);
        if (hasProducts)
        {
            throw new InvalidOperationException("This category has active products linked to it and cannot be deleted. Deactivate or reassign those products first.");
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }
}
