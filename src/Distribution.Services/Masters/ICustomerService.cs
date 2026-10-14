namespace Distribution.Services.Masters;

public interface ICustomerService
{
    Task<List<CustomerListItem>> SearchAsync(string? query, CancellationToken ct = default);
    Task<CustomerEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CustomerEditModel model, string performedBy, CancellationToken ct = default);
    Task UpdateAsync(CustomerEditModel model, string performedBy, CancellationToken ct = default);
    Task DeleteAsync(int id, string performedBy, CancellationToken ct = default);
}
