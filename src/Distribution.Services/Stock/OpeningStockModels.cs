namespace Distribution.Services.Stock;

public record OpeningStockEntryModel(int ProductId, DateTime AsOnDate, decimal Qty, decimal CostRate);

public record OpeningStockListItem(int Id, DateTime Date, string ProductCode, string ProductName, decimal Qty, decimal CostRate, decimal Value);
