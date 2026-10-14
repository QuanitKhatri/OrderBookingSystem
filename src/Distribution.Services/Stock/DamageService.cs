using Distribution.Data;
using Distribution.Domain.Common;
using Distribution.Domain.Stock;
using Distribution.Domain.Transactions;
using Distribution.Services.Sequencing;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Stock;

public class DamageService(DistributionDbContext db, IStockQueryService stockQuery, ISequenceService sequenceService) : IDamageService
{
    public async Task RecordAsync(DamageEntryModel model, string performedBy, CancellationToken ct = default)
    {
        var warehouseId = await stockQuery.GetDefaultWarehouseIdAsync(ct);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == model.ProductId, ct)
            ?? throw new InvalidOperationException("Product not found.");

        var currentQty = await stockQuery.GetCurrentStockAsync(model.ProductId, warehouseId, ct);
        if (model.Qty > currentQty)
        {
            throw new InsufficientStockException(product.Name, currentQty, model.Qty);
        }

        var refNo = await sequenceService.NextNumberAsync("DamageRefNo", "DMG-", ct);
        var lossAmount = model.Qty * product.AvgCost;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var entry = new DamageEntry
        {
            RefNo = refNo,
            Date = model.Date,
            ProductId = model.ProductId,
            WarehouseId = warehouseId,
            Qty = model.Qty,
            DamageReasonId = model.DamageReasonId,
            FoundAt = model.FoundAt,
            CostRate = product.AvgCost,
            LossAmount = lossAmount,
            Remarks = model.Remarks,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };
        db.DamageEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        db.StockMovements.Add(new StockMovement
        {
            Date = model.Date,
            ProductId = model.ProductId,
            WarehouseId = warehouseId,
            MovementType = MovementType.Damage,
            RefType = "Damage",
            RefId = entry.Id,
            QtyIn = 0,
            QtyOut = model.Qty,
            UnitCost = product.AvgCost
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<List<DamageListItem>> GetRecentAsync(int take = 100, CancellationToken ct = default)
    {
        return await db.DamageEntries
            .OrderByDescending(d => d.Id)
            .Take(take)
            .Select(d => new DamageListItem(
                d.Id, d.RefNo, d.Date, d.Product.Code, d.Product.Name, d.Qty,
                d.DamageReason.Name, d.FoundAt.ToString(), d.LossAmount))
            .ToListAsync(ct);
    }
}
