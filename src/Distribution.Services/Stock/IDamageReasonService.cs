namespace Distribution.Services.Stock;

public interface IDamageReasonService
{
    Task<List<(int Id, string Name)>> GetActiveLookupAsync(CancellationToken ct = default);
    Task<int> CreateAsync(string name, string performedBy, CancellationToken ct = default);
}
