namespace Distribution.Services.Stock;

public record PurchaseItemInput(int ProductId, decimal Qty, decimal Rate);

public record PurchaseInputModel(
    string InvoiceNo, int SupplierId, DateTime Date, string? VehicleNo, decimal BiltiCharges,
    string? Remarks, List<PurchaseItemInput> Items);

public record PurchaseItemResult(
    int ProductId, string ProductCode, string ProductName, decimal Qty, decimal Rate,
    decimal Amount, decimal AvgCostBefore, decimal AvgCostAfter);

public record PurchaseResult(int PurchaseId, string InvoiceNo, decimal TotalAmount, List<PurchaseItemResult> Items);

public record PurchaseListItem(int Id, string InvoiceNo, DateTime Date, string SupplierName, decimal TotalAmount, decimal BiltiCharges);
