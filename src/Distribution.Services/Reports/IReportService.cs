namespace Distribution.Services.Reports;

public interface IReportService
{
    Task<List<ClosingStockRow>> GetDailyClosingStockAsync(DateTime date, int? companyId, int? categoryId, CancellationToken ct = default);
    Task<List<DsrBillRow>> GetDsrByBillAsync(DateTime date, CancellationToken ct = default);
    Task<List<DsrProductRow>> GetDsrByProductAsync(DateTime date, CancellationToken ct = default);
    Task<List<DsrCompanyRow>> GetDsrByCompanyAsync(DateTime date, CancellationToken ct = default);
    Task<ProfitLossResult> GetProfitLossAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<List<DamageReportRow>> GetDamageReportAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
}
