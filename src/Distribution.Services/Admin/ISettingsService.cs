namespace Distribution.Services.Admin;

public record AppSettingsModel(
    string CompanyName,
    string CompanyAddress,
    string CompanyPhone,
    string InvoicePrefix,
    bool AllowNegativeStock,
    int DefaultWarehouseId);

public interface ISettingsService
{
    Task<AppSettingsModel> GetAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettingsModel model, string performedBy, CancellationToken ct = default);
}
