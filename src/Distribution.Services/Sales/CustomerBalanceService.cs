using Distribution.Data;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Sales;

public class CustomerBalanceService(DistributionDbContext db) : ICustomerBalanceService
{
    public async Task<decimal> GetBalanceAsync(int customerId, CancellationToken ct = default)
    {
        var customer = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId, ct);
        var sales = await db.Sales.Where(s => s.CustomerId == customerId).SumAsync(s => s.NetAmount, ct);
        var receipts = await db.CustomerReceipts.Where(r => r.CustomerId == customerId).SumAsync(r => r.Amount, ct);
        return customer.OpeningBalance + sales - receipts;
    }

    public async Task<Dictionary<int, decimal>> GetBalancesAsync(IEnumerable<int> customerIds, CancellationToken ct = default)
    {
        var ids = customerIds.ToList();

        var openingBalances = await db.Customers.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.OpeningBalance, ct);

        var salesTotals = await db.Sales.Where(s => ids.Contains(s.CustomerId))
            .GroupBy(s => s.CustomerId)
            .Select(g => new { CustomerId = g.Key, Total = g.Sum(s => s.NetAmount) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Total, ct);

        var receiptTotals = await db.CustomerReceipts.Where(r => ids.Contains(r.CustomerId))
            .GroupBy(r => r.CustomerId)
            .Select(g => new { CustomerId = g.Key, Total = g.Sum(r => r.Amount) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Total, ct);

        return ids.ToDictionary(
            id => id,
            id => openingBalances.GetValueOrDefault(id) + salesTotals.GetValueOrDefault(id) - receiptTotals.GetValueOrDefault(id));
    }

    public async Task<decimal> GetBalanceAsOfAsync(int customerId, DateTime asOfDate, CancellationToken ct = default)
    {
        var customer = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId, ct);
        var sales = await db.Sales.Where(s => s.CustomerId == customerId && s.Date < asOfDate).SumAsync(s => s.NetAmount, ct);
        var receipts = await db.CustomerReceipts.Where(r => r.CustomerId == customerId && r.Date < asOfDate).SumAsync(r => r.Amount, ct);
        return customer.OpeningBalance + sales - receipts;
    }
}
