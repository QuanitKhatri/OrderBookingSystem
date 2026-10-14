using Distribution.Data;
using Distribution.Domain.Common;
using Distribution.Domain.Stock;
using Distribution.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Stock;

public class PurchaseService(DistributionDbContext db, IStockQueryService stockQuery) : IPurchaseService
{
    private const string BiltiCategoryName = "Bilti / Freight";

    public async Task<PurchaseResult> CreateAsync(PurchaseInputModel model, string performedBy, CancellationToken ct = default)
    {
        if (model.Items.Count == 0)
        {
            throw new InvalidOperationException("At least one item is required.");
        }

        var warehouseId = await stockQuery.GetDefaultWarehouseIdAsync(ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var purchase = new Purchase
        {
            InvoiceNo = model.InvoiceNo.Trim(),
            SupplierId = model.SupplierId,
            WarehouseId = warehouseId,
            Date = model.Date,
            VehicleNo = model.VehicleNo,
            BiltiCharges = model.BiltiCharges,
            Remarks = model.Remarks,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };

        var itemResults = new List<PurchaseItemResult>();
        decimal totalAmount = 0;

        foreach (var item in model.Items)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, ct)
                ?? throw new InvalidOperationException($"Product {item.ProductId} not found.");

            var currentQty = await stockQuery.GetCurrentStockAsync(item.ProductId, warehouseId, ct);
            var avgCostBefore = product.AvgCost;
            var avgCostAfter = AvgCostCalculator.NewAverage(currentQty, avgCostBefore, item.Qty, item.Rate);
            var amount = item.Qty * item.Rate;

            purchase.Items.Add(new PurchaseItem
            {
                ProductId = item.ProductId,
                Qty = item.Qty,
                Rate = item.Rate,
                Amount = amount,
                AvgCostBefore = avgCostBefore,
                AvgCostAfter = avgCostAfter
            });

            product.AvgCost = avgCostAfter;
            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedBy = performedBy;

            itemResults.Add(new PurchaseItemResult(
                item.ProductId, product.Code, product.Name, item.Qty, item.Rate, amount, avgCostBefore, avgCostAfter));

            totalAmount += amount;
        }

        purchase.TotalAmount = totalAmount;
        db.Purchases.Add(purchase);
        await db.SaveChangesAsync(ct);

        foreach (var item in model.Items)
        {
            db.StockMovements.Add(new StockMovement
            {
                Date = model.Date,
                ProductId = item.ProductId,
                WarehouseId = warehouseId,
                MovementType = MovementType.Purchase,
                RefType = "Purchase",
                RefId = purchase.Id,
                QtyIn = item.Qty,
                QtyOut = 0,
                UnitCost = item.Rate
            });
        }

        if (model.BiltiCharges > 0)
        {
            var biltiCategory = await db.ExpenseCategories.FirstOrDefaultAsync(c => c.Name == BiltiCategoryName, ct);
            if (biltiCategory is null)
            {
                biltiCategory = new ExpenseCategory
                {
                    Name = BiltiCategoryName,
                    Type = ExpenseType.Business,
                    IsSystem = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = performedBy
                };
                db.ExpenseCategories.Add(biltiCategory);
                await db.SaveChangesAsync(ct);
            }

            db.Expenses.Add(new Expense
            {
                VoucherNo = $"BILTI-{purchase.Id}",
                Date = model.Date,
                ExpenseCategoryId = biltiCategory.Id,
                Amount = model.BiltiCharges,
                PaidFrom = PaidBy.Cash,
                PurchaseId = purchase.Id,
                Description = $"Bilti/freight for purchase {purchase.InvoiceNo}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = performedBy
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new PurchaseResult(purchase.Id, purchase.InvoiceNo, totalAmount, itemResults);
    }

    public async Task<List<PurchaseListItem>> GetTodaysPurchasesAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await db.Purchases
            .Where(p => p.Date.Date == today)
            .OrderByDescending(p => p.Id)
            .Select(p => new PurchaseListItem(p.Id, p.InvoiceNo, p.Date, p.Supplier.Name, p.TotalAmount, p.BiltiCharges))
            .ToListAsync(ct);
    }
}
