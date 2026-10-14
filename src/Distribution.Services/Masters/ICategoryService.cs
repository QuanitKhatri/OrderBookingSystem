namespace Distribution.Services.Masters;

public interface ICategoryService
{
    Task<List<CategoryListItem>> SearchAsync(string? query, CancellationToken ct = default);
    Task<CategoryEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<List<(int Id, string Name)>> GetActiveLookupAsync(CancellationToken ct = default);
    Task<int> CreateAsync(CategoryEditModel model, string performedBy, CancellationToken ct = default);
    Task UpdateAsync(CategoryEditModel model, string performedBy, CancellationToken ct = default);
    Task DeleteAsync(int id, string performedBy, CancellationToken ct = default);
}
