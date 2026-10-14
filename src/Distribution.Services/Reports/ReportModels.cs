namespace Distribution.Services.Reports;

public record ClosingStockRow(string Code, string ProductName, string CompanyName, string CategoryName, decimal Opening, decimal Received, decimal Sold, decimal Damage, decimal Closing, decimal AvgCost, decimal Value);

public record DsrBillRow(string BillNo, string CustomerName, decimal Gross, decimal Discount, decimal Net, decimal Cash, decimal Credit);
public record DsrProductRow(string ProductName, decimal Qty, decimal NetAmount);
public record DsrCompanyRow(string CompanyName, decimal NetAmount);

public record ProfitLossResult(
    decimal GrossSale, decimal Discount, decimal NetSale, decimal Cogs, decimal GrossProfit,
    decimal DamageLoss, decimal ProfitBeforeExpense, decimal BusinessExpenses, decimal SelfExpenses,
    decimal NetProfit, decimal NetAfterSelf);

public record DamageReportRow(DateTime Date, string ProductName, string CompanyName, string Reason, decimal Qty, decimal LossAmount);
