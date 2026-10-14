using Distribution.Data;
using Distribution.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Reports;

public class ReportService(DistributionDbContext db) : IReportService
{
    public async Task<List<ClosingStockRow>> GetDailyClosingStockAsync(DateTime date, int? companyId, int? categoryId, CancellationToken ct = default)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var productsQuery = db.Products.AsNoTracking().Include(p => p.Company).Include(p => p.Category).Where(p => !p.IsDeleted);
        if (companyId.HasValue) productsQuery = productsQuery.Where(p => p.CompanyId == companyId.Value);
        if (categoryId.HasValue) productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
        var products = await productsQuery.ToListAsync(ct);
        var productIds = products.Select(p => p.Id).ToList();

        var movements = await db.StockMovements
            .Where(m => productIds.Contains(m.ProductId) && m.Date < dayEnd)
            .ToListAsync(ct);

        var rows = new List<ClosingStockRow>();
        foreach (var product in products)
        {
            var before = movements.Where(m => m.ProductId == product.Id && m.Date < dayStart);
            var opening = before.Sum(m => m.QtyIn) - before.Sum(m => m.QtyOut);

            var onDay = movements.Where(m => m.ProductId == product.Id && m.Date >= dayStart && m.Date < dayEnd);
            var received = onDay.Where(m => m.MovementType is MovementType.Purchase or MovementType.Opening).Sum(m => m.QtyIn);
            var sold = onDay.Where(m => m.MovementType == MovementType.Sale).Sum(m => m.QtyOut);
            var damage = onDay.Where(m => m.MovementType == MovementType.Damage).Sum(m => m.QtyOut);

            var closing = opening + received - sold - damage;

            rows.Add(new ClosingStockRow(
                product.Code, product.Name, product.Company.Name, product.Category.Name,
                opening, received, sold, damage, closing, product.AvgCost, closing * product.AvgCost));
        }

        return rows;
    }

    public async Task<List<DsrBillRow>> GetDsrByBillAsync(DateTime date, CancellationToken ct = default)
    {
        var day = date.Date;
        return await db.Sales.Include(s => s.Customer)
            .Where(s => s.Date.Date == day)
            .Select(s => new DsrBillRow(s.BillNo, s.Customer.ShopName, s.GrossAmount, s.ItemDiscount + s.BillDiscount, s.NetAmount, s.CashReceived, s.NetAmount - s.CashReceived))
            .ToListAsync(ct);
    }

    public async Task<List<DsrProductRow>> GetDsrByProductAsync(DateTime date, CancellationToken ct = default)
    {
        var day = date.Date;
        // EF Core cannot translate GroupBy-by-navigation-member combined with multiple Sum()
        // aggregates in the same Select; project the scalar fields server-side first, then
        // group/aggregate in memory (same safe pattern as GetDsrByBillAsync).
        var rows = await db.SaleItems.AsNoTracking()
            .Where(i => i.Sale.Date.Date == day)
            .Select(i => new { ProductName = i.Product.Name, i.Qty, i.NetAmount })
            .ToListAsync(ct);

        return rows.GroupBy(r => r.ProductName)
            .Select(g => new DsrProductRow(g.Key, g.Sum(r => r.Qty), g.Sum(r => r.NetAmount)))
            .OrderByDescending(r => r.NetAmount)
            .ToList();
    }

    public async Task<List<DsrCompanyRow>> GetDsrByCompanyAsync(DateTime date, CancellationToken ct = default)
    {
        var day = date.Date;
        var rows = await db.SaleItems.AsNoTracking()
            .Where(i => i.Sale.Date.Date == day)
            .Select(i => new { CompanyName = i.Product.Company.Name, i.NetAmount })
            .ToListAsync(ct);

        return rows.GroupBy(r => r.CompanyName)
            .Select(g => new DsrCompanyRow(g.Key, g.Sum(r => r.NetAmount)))
            .OrderByDescending(r => r.NetAmount)
            .ToList();
    }

    public async Task<ProfitLossResult> GetProfitLossAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1);

        var sales = await db.Sales.AsNoTracking().Where(s => s.Date >= from && s.Date < to).ToListAsync(ct);
        var grossSale = sales.Sum(s => s.GrossAmount);
        var discount = sales.Sum(s => s.ItemDiscount + s.BillDiscount);
        var netSale = grossSale - discount;

        var cogs = await db.SaleItems.Include(i => i.Sale)
            .Where(i => i.Sale.Date >= from && i.Sale.Date < to)
            .SumAsync(i => i.Qty * i.CostRate, ct);

        var grossProfit = netSale - cogs;

        var damageLoss = await db.DamageEntries.Where(d => d.Date >= from && d.Date < to).SumAsync(d => d.LossAmount, ct);
        var profitBeforeExpense = grossProfit - damageLoss;

        var expenses = await db.Expenses.AsNoTracking().Include(e => e.ExpenseCategory)
            .Where(e => e.Date >= from && e.Date < to)
            .ToListAsync(ct);
        var businessExpenses = expenses.Where(e => e.ExpenseCategory.Type == ExpenseType.Business).Sum(e => e.Amount);
        var selfExpenses = expenses.Where(e => e.ExpenseCategory.Type == ExpenseType.Self).Sum(e => e.Amount);

        var netProfit = profitBeforeExpense - businessExpenses;
        var netAfterSelf = netProfit - selfExpenses;

        return new ProfitLossResult(grossSale, discount, netSale, cogs, grossProfit, damageLoss, profitBeforeExpense, businessExpenses, selfExpenses, netProfit, netAfterSelf);
    }

    public async Task<List<DamageReportRow>> GetDamageReportAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1);

        return await db.DamageEntries
            .Include(d => d.Product).ThenInclude(p => p.Company)
            .Include(d => d.DamageReason)
            .Where(d => d.Date >= from && d.Date < to)
            .Select(d => new DamageReportRow(d.Date, d.Product.Name, d.Product.Company.Name, d.DamageReason.Name, d.Qty, d.LossAmount))
            .ToListAsync(ct);
    }
}
