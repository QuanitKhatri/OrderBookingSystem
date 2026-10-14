using Distribution.Data;
using Distribution.Domain.System;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Admin;

public class SettingsService(DistributionDbContext db) : ISettingsService
{
    public async Task<AppSettingsModel> GetAsync(CancellationToken ct = default)
    {
        var rows = await db.Settings.ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        return new AppSettingsModel(
            rows.GetValueOrDefault("CompanyName", ""),
            rows.GetValueOrDefault("CompanyAddress", ""),
            rows.GetValueOrDefault("CompanyPhone", ""),
            rows.GetValueOrDefault("InvoicePrefix", "BILL-"),
            rows.GetValueOrDefault("AllowNegativeStock", "false") == "true",
            int.TryParse(rows.GetValueOrDefault("DefaultWarehouseId", "0"), out var wid) ? wid : 0);
    }

    public async Task SaveAsync(AppSettingsModel model, string performedBy, CancellationToken ct = default)
    {
        var values = new Dictionary<string, string>
        {
            ["CompanyName"] = model.CompanyName,
            ["CompanyAddress"] = model.CompanyAddress,
            ["CompanyPhone"] = model.CompanyPhone,
            ["InvoicePrefix"] = model.InvoicePrefix,
            ["AllowNegativeStock"] = model.AllowNegativeStock ? "true" : "false",
            ["DefaultWarehouseId"] = model.DefaultWarehouseId.ToString()
        };

        var existing = await db.Settings.ToDictionaryAsync(s => s.Key, s => s, ct);

        foreach (var (key, value) in values)
        {
            if (existing.TryGetValue(key, out var row))
            {
                row.Value = value;
            }
            else
            {
                db.Settings.Add(new Setting { Key = key, Value = value });
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
