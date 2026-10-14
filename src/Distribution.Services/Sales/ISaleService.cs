namespace Distribution.Services.Sales;

public interface ISaleService
{
    Task<decimal> GetCustomerBalanceAsync(int customerId, CancellationToken ct = default);
    Task<SaleResult> CreateAsync(SaleInputModel model, bool isAdmin, string performedBy, CancellationToken ct = default);
    Task<List<SaleListItem>> GetTodaysSalesAsync(CancellationToken ct = default);
}

public interface ICustomerLedgerService
{
    Task<decimal> GetOpeningBalanceAsync(int customerId, DateTime fromDate, CancellationToken ct = default);
    Task<List<LedgerEntry>> GetLedgerAsync(int customerId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
}

public interface IMarketCreditService
{
    Task<List<MarketCreditItem>> GetOutstandingAsync(CancellationToken ct = default);
}
