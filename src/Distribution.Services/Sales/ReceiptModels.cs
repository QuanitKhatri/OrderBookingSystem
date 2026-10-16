using Distribution.Domain.Common;

namespace Distribution.Services.Sales;

public record ReceiptInputModel(int CustomerId, DateTime Date, decimal Amount, PaidBy PaidBy, string? Remarks);

public record ReceiptResult(int ReceiptId, string ReceiptNo, decimal PreviousBalance, decimal NewBalance);

/// <summary>A received payment; <see cref="BillNo"/> is set when the money was taken as Cash Received on a bill.</summary>
public record ReceiptListItem(int Id, string ReceiptNo, DateTime Date, string CustomerCode, string CustomerName, decimal Amount, string PaidBy, string? BillNo, string? Remarks);
