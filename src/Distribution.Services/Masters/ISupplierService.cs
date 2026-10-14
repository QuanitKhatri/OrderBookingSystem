namespace Distribution.Services.Masters;

public interface ISupplierService
{
    Task<List<SupplierListItem>> SearchAsync(string? query, CancellationToken ct = default);
    Task<SupplierEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(SupplierEditModel model, string performedBy, CancellationToken ct = default);
    Task UpdateAsync(SupplierEditModel model, string performedBy, CancellationToken ct = default);
    Task DeleteAsync(int id, string performedBy, CancellationToken ct = default);
}
