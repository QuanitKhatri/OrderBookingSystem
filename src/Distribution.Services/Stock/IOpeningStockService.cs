namespace Distribution.Services.Stock;

public interface IOpeningStockService
{
    Task RecordAsync(OpeningStockEntryModel model, string performedBy, CancellationToken ct = default);
    Task<List<OpeningStockListItem>> GetRecentAsync(int take = 50, CancellationToken ct = default);
}
