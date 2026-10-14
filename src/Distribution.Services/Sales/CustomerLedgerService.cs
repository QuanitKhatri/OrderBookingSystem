using Distribution.Data;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Sales;

public class CustomerLedgerService(DistributionDbContext db, ICustomerBalanceService balanceService) : ICustomerLedgerService
{
    public Task<decimal> GetOpeningBalanceAsync(int customerId, DateTime fromDate, CancellationToken ct = default) =>
        balanceService.GetBalanceAsOfAsync(customerId, fromDate, ct);

    public async Task<List<LedgerEntry>> GetLedgerAsync(int customerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var running = await GetOpeningBalanceAsync(customerId, fromDate, ct);

        var sales = await db.Sales
            .Where(s => s.CustomerId == customerId && s.Date >= fromDate && s.Date <= toDate)
            .Select(s => new { s.Date, Reference = s.BillNo, Amount = s.NetAmount })
            .ToListAsync(ct);

        var receipts = await db.CustomerReceipts
            .Where(r => r.CustomerId == customerId && r.Date >= fromDate && r.Date <= toDate)
            .Select(r => new { r.Date, Reference = r.ReceiptNo, r.Amount })
            .ToListAsync(ct);

        var combined = sales.Select(s => (s.Date, Type: "Sale", s.Reference, Debit: s.Amount, Credit: 0m))
            .Concat(receipts.Select(r => (r.Date, Type: "Receipt", r.Reference, Debit: 0m, Credit: r.Amount)))
            .OrderBy(x => x.Date)
            .ToList();

        var result = new List<LedgerEntry>
        {
            new(fromDate, "Opening", "Opening Balance", 0, 0, running)
        };

        foreach (var entry in combined)
        {
            running += entry.Debit - entry.Credit;
            result.Add(new LedgerEntry(entry.Date, entry.Type, entry.Reference, entry.Debit, entry.Credit, running));
        }

        return result;
    }
}

public class MarketCreditService(DistributionDbContext db, ICustomerBalanceService balanceService) : IMarketCreditService
{
    public async Task<List<MarketCreditItem>> GetOutstandingAsync(CancellationToken ct = default)
    {
        var customers = await db.Customers.AsNoTracking().Include(c => c.Area).Where(c => !c.IsDeleted).ToListAsync(ct);
        var ids = customers.Select(c => c.Id).ToList();

        var balances = await balanceService.GetBalancesAsync(ids, ct);

        var lastReceiptDates = await db.CustomerReceipts.Where(r => ids.Contains(r.CustomerId))
            .GroupBy(r => r.CustomerId)
            .Select(g => new { CustomerId = g.Key, Last = g.Max(r => r.Date) })
            .ToDictionaryAsync(x => x.CustomerId, x => (DateTime?)x.Last, ct);

        return customers
            .Select(c => new MarketCreditItem(c.Id, c.Code, c.ShopName, c.Area.Name, balances.GetValueOrDefault(c.Id), lastReceiptDates.GetValueOrDefault(c.Id)))
            .Where(x => x.Balance > 0)
            .OrderByDescending(x => x.Balance)
            .ToList();
    }
}
