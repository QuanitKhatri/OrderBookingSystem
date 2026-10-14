using Distribution.Data;
using Distribution.Domain.Masters;
using Distribution.Services.Sales;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Masters;

public class CustomerService(DistributionDbContext db, ICustomerBalanceService balanceService) : ICustomerService
{
    public async Task<List<CustomerListItem>> SearchAsync(string? query, CancellationToken ct = default)
    {
        var q = db.Customers.AsNoTracking().Include(c => c.Area).Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(c => EF.Functions.ILike(c.ShopName, $"%{term}%") || EF.Functions.ILike(c.Code, $"%{term}%"));
        }

        var customers = await q.OrderBy(c => c.ShopName).ToListAsync(ct);
        var balances = await balanceService.GetBalancesAsync(customers.Select(c => c.Id), ct);

        return customers.Select(c =>
            new CustomerListItem(c.Id, c.Code, c.ShopName, c.Area.Name, balances.GetValueOrDefault(c.Id), c.IsActive)).ToList();
    }

    public async Task<CustomerEditModel?> GetAsync(int id, CancellationToken ct = default)
    {
        return await db.Customers.Where(c => c.Id == id)
            .Select(c => new CustomerEditModel(c.Id, c.Code, c.ShopName, c.OwnerName, c.Phone, c.AreaId, c.Address, c.OpeningBalance, c.DefaultDiscountPct))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CreateAsync(CustomerEditModel model, string performedBy, CancellationToken ct = default)
    {
        var entity = new Customer
        {
            Code = model.Code.Trim(),
            ShopName = model.ShopName.Trim(),
            OwnerName = model.OwnerName,
            Phone = model.Phone,
            AreaId = model.AreaId,
            Address = model.Address,
            OpeningBalance = model.OpeningBalance,
            DefaultDiscountPct = model.DefaultDiscountPct,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };
        db.Customers.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(CustomerEditModel model, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Customers.FirstOrDefaultAsync(c => c.Id == model.Id, ct)
            ?? throw new InvalidOperationException("Customer not found.");

        entity.Code = model.Code.Trim();
        entity.ShopName = model.ShopName.Trim();
        entity.OwnerName = model.OwnerName;
        entity.Phone = model.Phone;
        entity.AreaId = model.AreaId;
        entity.Address = model.Address;
        entity.OpeningBalance = model.OpeningBalance;
        entity.DefaultDiscountPct = model.DefaultDiscountPct;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException("Customer not found.");

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }
}
