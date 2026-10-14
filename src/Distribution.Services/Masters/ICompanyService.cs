namespace Distribution.Services.Masters;

public interface ICompanyService
{
    Task<List<CompanyListItem>> SearchAsync(string? query, CancellationToken ct = default);
    Task<CompanyEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<List<(int Id, string Name)>> GetActiveLookupAsync(CancellationToken ct = default);
    Task<int> CreateAsync(CompanyEditModel model, string performedBy, CancellationToken ct = default);
    Task UpdateAsync(CompanyEditModel model, string performedBy, CancellationToken ct = default);
    Task DeleteAsync(int id, string performedBy, CancellationToken ct = default);
}
