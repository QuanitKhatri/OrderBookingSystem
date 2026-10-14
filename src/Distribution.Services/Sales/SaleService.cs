using Distribution.Data;
using Distribution.Domain.Common;
using Distribution.Domain.Transactions;
using Distribution.Services.Sequencing;
using Distribution.Services.Stock;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Sales;

public class SaleService(DistributionDbContext db, IStockQueryService stockQuery, ISequenceService sequenceService, ICustomerBalanceService balanceService) : ISaleService
{
    private static decimal Round0(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);

    public Task<decimal> GetCustomerBalanceAsync(int customerId, CancellationToken ct = default) =>
        balanceService.GetBalanceAsync(customerId, ct);

    public async Task<SaleResult> CreateAsync(SaleInputModel model, bool isAdmin, string performedBy, CancellationToken ct = default)
    {
        if (model.Items.Count == 0)
        {
            throw new InvalidOperationException("Add at least one item.");
        }
        if (model.BillDiscount < 0)
        {
            throw new InvalidOperationException("Bill discount cannot be negative.");
        }
        if (model.CashReceived < 0)
        {
            throw new InvalidOperationException("Cash received cannot be negative.");
        }

        var warehouseId = await stockQuery.GetDefaultWarehouseIdAsync(ct);
        var previousBalance = await GetCustomerBalanceAsync(model.CustomerId, ct);

        var lineResults = new List<SaleLineResult>();
        var sale = new Domain.Transactions.Sale
        {
            CustomerId = model.CustomerId,
            WarehouseId = warehouseId,
            Date = model.Date,
            PreviousBalance = previousBalance,
            CashReceived = model.CashReceived
        };

        decimal gross = 0, itemDiscount = 0;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        foreach (var line in model.Items)
        {
            if (line.Qty <= 0)
            {
                throw new InvalidOperationException("Quantity must be greater than zero.");
            }
            if (line.Rate <= 0)
            {
                throw new InvalidOperationException("Rate must be greater than zero.");
            }
            if (line.DiscountPct < 0 || line.DiscountPct > 100)
            {
                throw new InvalidOperationException("Discount % must be between 0 and 100.");
            }

            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == line.ProductId, ct)
                ?? throw new InvalidOperationException($"Product {line.ProductId} not found.");

            if (!isAdmin && line.DiscountPct > product.MaxDiscountPct)
            {
                throw new InvalidOperationException($"Discount {line.DiscountPct}% exceeds max allowed {product.MaxDiscountPct}% for {product.Name}.");
            }

            var available = await stockQuery.GetCurrentStockAsync(line.ProductId, warehouseId, ct);
            if (line.Qty > available)
            {
                throw new InsufficientStockException(product.Name, available, line.Qty);
            }

            var amount = Round0(line.Qty * line.Rate);
            var discountAmount = Round0(amount * line.DiscountPct / 100);
            var netAmount = amount - discountAmount;

            sale.Items.Add(new SaleItem
            {
                ProductId = line.ProductId,
                Qty = line.Qty,
                Rate = line.Rate,
                Amount = amount,
                DiscountPct = line.DiscountPct,
                DiscountAmount = discountAmount,
                NetAmount = netAmount,
                CostRate = product.AvgCost
            });

            lineResults.Add(new SaleLineResult(line.ProductId, product.Code, product.Name, line.Qty, line.Rate, amount, discountAmount, netAmount));

            gross += amount;
            itemDiscount += discountAmount;
        }

        var billDiscount = Round0(model.BillDiscount);
        var netTotal = gross - itemDiscount - billDiscount;

        sale.BillNo = await sequenceService.NextNumberAsync("BillNo", "BILL-", ct);
        sale.GrossAmount = gross;
        sale.ItemDiscount = itemDiscount;
        sale.BillDiscount = billDiscount;
        sale.NetAmount = netTotal;
        sale.CreatedAt = DateTime.UtcNow;
        sale.CreatedBy = performedBy;

        db.Sales.Add(sale);
        await db.SaveChangesAsync(ct);

        foreach (var line in model.Items)
        {
            db.StockMovements.Add(new Domain.Stock.StockMovement
            {
                Date = model.Date,
                ProductId = line.ProductId,
                WarehouseId = warehouseId,
                MovementType = MovementType.Sale,
                RefType = "Sale",
                RefId = sale.Id,
                QtyIn = 0,
                QtyOut = line.Qty,
                UnitCost = sale.Items.First(i => i.ProductId == line.ProductId).CostRate
            });
        }

        if (model.CashReceived > 0)
        {
            db.CustomerReceipts.Add(new CustomerReceipt
            {
                ReceiptNo = await sequenceService.NextNumberAsync("ReceiptNo", "RCPT-", ct),
                CustomerId = model.CustomerId,
                Date = model.Date,
                Amount = model.CashReceived,
                PaidBy = PaidBy.Cash,
                SaleId = sale.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = performedBy
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var newBalance = previousBalance + netTotal - model.CashReceived;

        return new SaleResult(sale.Id, sale.BillNo, gross, itemDiscount, billDiscount, netTotal, previousBalance, newBalance, lineResults);
    }

    public async Task<List<SaleListItem>> GetTodaysSalesAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await db.Sales.Where(s => s.Date.Date == today)
            .OrderByDescending(s => s.Id)
            .Select(s => new SaleListItem(s.Id, s.BillNo, s.Date, s.Customer.ShopName, s.NetAmount, s.CashReceived))
            .ToListAsync(ct);
    }
}
