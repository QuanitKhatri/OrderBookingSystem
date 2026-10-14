namespace Distribution.Services.Sales;

/// <summary>Single source of truth for the "OpeningBalance + Sales − Receipts" customer-balance formula,
/// previously duplicated independently across SaleService, CustomerService, MarketCreditService and CustomerLedgerService.</summary>
public interface ICustomerBalanceService
{
    Task<decimal> GetBalanceAsync(int customerId, CancellationToken ct = default);
    Task<Dictionary<int, decimal>> GetBalancesAsync(IEnumerable<int> customerIds, CancellationToken ct = default);
    Task<decimal> GetBalanceAsOfAsync(int customerId, DateTime asOfDate, CancellationToken ct = default);
}
