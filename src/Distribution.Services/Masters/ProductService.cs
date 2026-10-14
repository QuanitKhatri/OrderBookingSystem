using ClosedXML.Excel;
using Distribution.Data;
using Distribution.Domain.Common;
using Distribution.Domain.Masters;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Masters;

public class ProductService(DistributionDbContext db) : IProductService
{
    public async Task<List<(int Id, string Code, string Name)>> GetActiveLookupAsync(CancellationToken ct = default)
    {
        return await db.Products.Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Select(p => new ValueTuple<int, string, string>(p.Id, p.Code, p.Name))
            .ToListAsync(ct);
    }

    public async Task<List<ProductListItem>> SearchAsync(string? query, int? companyId, int? categoryId, CancellationToken ct = default)
    {
        var q = db.Products.AsNoTracking().Include(p => p.Company).Include(p => p.Category).Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(p => EF.Functions.ILike(p.Name, $"%{term}%")
                || EF.Functions.ILike(p.Code, $"%{term}%")
                || (p.Barcode != null && EF.Functions.ILike(p.Barcode, $"%{term}%")));
        }

        if (companyId.HasValue)
        {
            q = q.Where(p => p.CompanyId == companyId.Value);
        }

        if (categoryId.HasValue)
        {
            q = q.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await q.OrderBy(p => p.Name).ToListAsync(ct);
        var ids = products.Select(p => p.Id).ToList();

        var stockByProduct = await db.StockMovements.Where(m => ids.Contains(m.ProductId))
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(m => m.QtyIn) - g.Sum(m => m.QtyOut) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, ct);

        return products.Select(p => new ProductListItem(
            p.Id, p.Code, p.Name, p.Company.Name, p.Category.Name,
            p.AvgCost, p.SaleRate, stockByProduct.GetValueOrDefault(p.Id), p.IsActive)).ToList();
    }

    public async Task<ProductEditModel?> GetAsync(int id, CancellationToken ct = default)
    {
        return await db.Products.Where(p => p.Id == id)
            .Select(p => new ProductEditModel(p.Id, p.Code, p.Name, p.CompanyId, p.CategoryId, p.Unit, p.PcsPerUnit, p.PurchaseRate, p.SaleRate, p.MaxDiscountPct, p.Barcode))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CreateAsync(ProductEditModel model, string performedBy, CancellationToken ct = default)
    {
        var entity = new Product
        {
            Code = model.Code.Trim(),
            Name = model.Name.Trim(),
            CompanyId = model.CompanyId,
            CategoryId = model.CategoryId,
            Unit = model.Unit,
            PcsPerUnit = model.PcsPerUnit,
            PurchaseRate = model.PurchaseRate,
            SaleRate = model.SaleRate,
            AvgCost = model.PurchaseRate,
            MaxDiscountPct = model.MaxDiscountPct,
            Barcode = model.Barcode,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };
        db.Products.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(ProductEditModel model, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Products.FirstOrDefaultAsync(p => p.Id == model.Id, ct)
            ?? throw new InvalidOperationException("Product not found.");

        entity.Code = model.Code.Trim();
        entity.Name = model.Name.Trim();
        entity.CompanyId = model.CompanyId;
        entity.CategoryId = model.CategoryId;
        entity.Unit = model.Unit;
        entity.PcsPerUnit = model.PcsPerUnit;
        entity.PurchaseRate = model.PurchaseRate;
        entity.SaleRate = model.SaleRate;
        entity.MaxDiscountPct = model.MaxDiscountPct;
        // Barcode is no longer editable from this form (only via Excel import) — never clobber it here.
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new InvalidOperationException("Product not found.");

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }

    public async Task<ProductImportResult> ImportFromExcelAsync(Stream fileStream, string performedBy, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(fileStream);
        var sheet = workbook.Worksheets.First();
        var headerRow = sheet.Row(1);

        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            columns[cell.GetString().Trim()] = cell.Address.ColumnNumber;
        }

        string[] required = ["Code", "Name", "Company", "Category", "Unit", "PcsPerUnit", "PurchaseRate", "SaleRate"];
        var missing = required.Where(r => !columns.ContainsKey(r)).ToList();
        if (missing.Count > 0)
        {
            return new ProductImportResult(0, 0, [new ProductImportRow(1, $"Missing required column(s): {string.Join(", ", missing)}")]);
        }

        var companies = (await db.Companies.Where(c => !c.IsDeleted).Select(c => new { c.Id, c.Name }).ToListAsync(ct))
            .ToDictionary(c => c.Name.Trim(), c => c.Id, StringComparer.OrdinalIgnoreCase);
        var categories = (await db.Categories.Where(c => !c.IsDeleted).Select(c => new { c.Id, c.Name }).ToListAsync(ct))
            .ToDictionary(c => c.Name.Trim(), c => c.Id, StringComparer.OrdinalIgnoreCase);
        var existingProducts = await db.Products.ToDictionaryAsync(p => p.Code, p => p, StringComparer.OrdinalIgnoreCase, ct);

        var errors = new List<ProductImportRow>();
        int imported = 0, updated = 0;

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = sheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            string Get(string col) => row.Cell(columns[col]).GetString().Trim();

            var code = Get("Code");
            var name = Get("Name");
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                errors.Add(new ProductImportRow(rowNum, "Code and Name are required."));
                continue;
            }

            var companyName = Get("Company");
            if (!companies.TryGetValue(companyName, out var companyId))
            {
                errors.Add(new ProductImportRow(rowNum, $"Unknown company '{companyName}'. Add it under Masters > Companies first."));
                continue;
            }

            var categoryName = Get("Category");
            if (!categories.TryGetValue(categoryName, out var categoryId))
            {
                errors.Add(new ProductImportRow(rowNum, $"Unknown category '{categoryName}'. Add it under Masters > Categories first."));
                continue;
            }

            if (!Enum.TryParse<UnitType>(Get("Unit"), ignoreCase: true, out var unit))
            {
                errors.Add(new ProductImportRow(rowNum, $"Unit must be one of: {string.Join(", ", Enum.GetNames<UnitType>())}."));
                continue;
            }

            if (!decimal.TryParse(Get("PcsPerUnit"), out var pcsPerUnit) ||
                !decimal.TryParse(Get("PurchaseRate"), out var purchaseRate) ||
                !decimal.TryParse(Get("SaleRate"), out var saleRate))
            {
                errors.Add(new ProductImportRow(rowNum, "PcsPerUnit, PurchaseRate and SaleRate must be numeric."));
                continue;
            }

            decimal maxDiscountPct = 0;
            if (columns.TryGetValue("MaxDiscountPct", out var maxDiscCol))
            {
                decimal.TryParse(row.Cell(maxDiscCol).GetString().Trim(), out maxDiscountPct);
            }

            string? barcode = columns.TryGetValue("Barcode", out var barcodeCol) ? row.Cell(barcodeCol).GetString().Trim() : null;

            if (existingProducts.TryGetValue(code, out var existing))
            {
                existing.Name = name;
                existing.CompanyId = companyId;
                existing.CategoryId = categoryId;
                existing.Unit = unit;
                existing.PcsPerUnit = pcsPerUnit;
                existing.PurchaseRate = purchaseRate;
                existing.SaleRate = saleRate;
                existing.MaxDiscountPct = maxDiscountPct;
                existing.Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = performedBy;
                updated++;
            }
            else
            {
                var entity = new Product
                {
                    Code = code,
                    Name = name,
                    CompanyId = companyId,
                    CategoryId = categoryId,
                    Unit = unit,
                    PcsPerUnit = pcsPerUnit,
                    PurchaseRate = purchaseRate,
                    SaleRate = saleRate,
                    AvgCost = purchaseRate,
                    MaxDiscountPct = maxDiscountPct,
                    Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = performedBy
                };
                db.Products.Add(entity);
                existingProducts[code] = entity;
                imported++;
            }
        }

        await db.SaveChangesAsync(ct);
        return new ProductImportResult(imported, updated, errors);
    }
}
