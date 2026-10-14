using Distribution.Domain.Common;

namespace Distribution.Services.Masters;

public record ProductListItem(int Id, string Code, string Name, string CompanyName, string CategoryName, decimal AvgCost, decimal SaleRate, decimal InStock, bool IsActive);

public record ProductEditModel(
    int Id,
    string Code,
    string Name,
    int CompanyId,
    int CategoryId,
    UnitType Unit,
    decimal PcsPerUnit,
    decimal PurchaseRate,
    decimal SaleRate,
    decimal MaxDiscountPct,
    string? Barcode);

public record ProductImportRow(int RowNumber, string? Error);

public record ProductImportResult(int Imported, int Updated, List<ProductImportRow> Errors);
