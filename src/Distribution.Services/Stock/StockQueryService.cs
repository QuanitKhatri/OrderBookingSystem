using Distribution.Data;
using Distribution.Domain.Masters;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Stock;

public class StockQueryService(DistributionDbContext db) : IStockQueryService
{
    public async Task<decimal> GetCurrentStockAsync(int productId, int warehouseId, CancellationToken ct = default)
    {
        var qtyIn = await db.StockMovements
            .Where(m => m.ProductId == productId && m.WarehouseId == warehouseId)
            .SumAsync(m => m.QtyIn, ct);
        var qtyOut = await db.StockMovements
            .Where(m => m.ProductId == productId && m.WarehouseId == warehouseId)
            .SumAsync(m => m.QtyOut, ct);
        return qtyIn - qtyOut;
    }

    public async Task<int> GetDefaultWarehouseIdAsync(CancellationToken ct = default)
    {
        var existing = await db.Warehouses.Where(w => w.IsActive && !w.IsDeleted)
            .OrderBy(w => w.Id)
            .Select(w => w.Id)
            .FirstOrDefaultAsync(ct);

        if (existing != 0) return existing;

        var warehouse = new Warehouse
        {
            Name = "Main Warehouse",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(ct);
        return warehouse.Id;
    }
}
