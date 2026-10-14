namespace Distribution.Services.Stock;

public interface IStockQueryService
{
    Task<decimal> GetCurrentStockAsync(int productId, int warehouseId, CancellationToken ct = default);
    Task<int> GetDefaultWarehouseIdAsync(CancellationToken ct = default);
}
