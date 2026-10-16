using Distribution.Domain.Common;

namespace Distribution.Services.Sales;

public record SaleLineInput(int ProductId, decimal Qty, decimal Rate, decimal DiscountPct);

public record SaleInputModel(int CustomerId, DateTime Date, List<SaleLineInput> Items, decimal BillDiscount, decimal CashReceived, PaidBy PaidBy = PaidBy.Cash);

public record SaleLineResult(int ProductId, string ProductCode, string ProductName, decimal Qty, decimal Rate, decimal Amount, decimal DiscountAmount, decimal NetAmount);

public record SaleResult(int SaleId, string BillNo, decimal GrossAmount, decimal ItemDiscount, decimal BillDiscount, decimal NetAmount, decimal PreviousBalance, decimal NewBalance, List<SaleLineResult> Items);

public record SaleListItem(int Id, string BillNo, DateTime Date, string CustomerName, decimal NetAmount, decimal CashReceived);

public record LedgerEntry(DateTime Date, string Type, string Reference, decimal Debit, decimal Credit, decimal RunningBalance);

public record MarketCreditItem(int CustomerId, string Code, string ShopName, string AreaName, decimal Balance, DateTime? LastPaymentDate);
