using Distribution.Domain.Common;

namespace Distribution.Services.Stock;

public record DamageEntryModel(int ProductId, DateTime Date, decimal Qty, int DamageReasonId, FoundAt FoundAt, string? Remarks);

public record DamageListItem(int Id, string RefNo, DateTime Date, string ProductCode, string ProductName, decimal Qty, string Reason, string FoundAt, decimal LossAmount);
