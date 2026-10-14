using Distribution.Data;
using Distribution.Domain.Common;
using Distribution.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Stock;

public class OpeningStockService(DistributionDbContext db, IStockQueryService stockQuery) : IOpeningStockService
{
    public async Task RecordAsync(OpeningStockEntryModel model, string performedBy, CancellationToken ct = default)
    {
        var warehouseId = await stockQuery.GetDefaultWarehouseIdAsync(ct);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == model.ProductId, ct)
            ?? throw new InvalidOperationException("Product not found.");

        var currentQty = await stockQuery.GetCurrentStockAsync(model.ProductId, warehouseId, ct);
        var newAvgCost = AvgCostCalculator.NewAverage(currentQty, product.AvgCost, model.Qty, model.CostRate);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.StockMovements.Add(new StockMovement
        {
            Date = model.AsOnDate,
            ProductId = model.ProductId,
            WarehouseId = warehouseId,
            MovementType = MovementType.Opening,
            RefType = "Opening",
            RefId = 0,
            QtyIn = model.Qty,
            QtyOut = 0,
            UnitCost = model.CostRate
        });

        product.AvgCost = newAvgCost;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<List<OpeningStockListItem>> GetRecentAsync(int take = 50, CancellationToken ct = default)
    {
        return await db.StockMovements
            .Where(m => m.MovementType == MovementType.Opening)
            .OrderByDescending(m => m.Id)
            .Take(take)
            .Select(m => new OpeningStockListItem(m.Id, m.Date, m.Product.Code, m.Product.Name, m.QtyIn, m.UnitCost, m.QtyIn * m.UnitCost))
            .ToListAsync(ct);
    }
}
