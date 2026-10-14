namespace Distribution.Services.Masters;

public interface IProductService
{
    Task<List<ProductListItem>> SearchAsync(string? query, int? companyId, int? categoryId, CancellationToken ct = default);
    Task<List<(int Id, string Code, string Name)>> GetActiveLookupAsync(CancellationToken ct = default);
    Task<ProductEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(ProductEditModel model, string performedBy, CancellationToken ct = default);
    Task UpdateAsync(ProductEditModel model, string performedBy, CancellationToken ct = default);
    Task DeleteAsync(int id, string performedBy, CancellationToken ct = default);
    Task<ProductImportResult> ImportFromExcelAsync(Stream fileStream, string performedBy, CancellationToken ct = default);
}
