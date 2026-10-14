namespace Distribution.Services.Stock;

public class InsufficientStockException(string productName, decimal available, decimal requested)
    : InvalidOperationException($"Insufficient stock for '{productName}'. Available: {available}, requested: {requested}.")
{
    public decimal Available { get; } = available;
    public decimal Requested { get; } = requested;
}

public interface IDamageService
{
    Task RecordAsync(DamageEntryModel model, string performedBy, CancellationToken ct = default);
    Task<List<DamageListItem>> GetRecentAsync(int take = 100, CancellationToken ct = default);
}
