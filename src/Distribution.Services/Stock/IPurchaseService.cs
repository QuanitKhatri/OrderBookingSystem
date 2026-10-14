namespace Distribution.Services.Stock;

public interface IPurchaseService
{
    Task<PurchaseResult> CreateAsync(PurchaseInputModel model, string performedBy, CancellationToken ct = default);
    Task<List<PurchaseListItem>> GetTodaysPurchasesAsync(CancellationToken ct = default);
}
