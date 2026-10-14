using Distribution.Domain.Common;

namespace Distribution.Services.Expenses;

public record ExpenseInputModel(DateTime Date, int CategoryId, decimal Amount, PaidBy PaidFrom, string? Description);

public record ExpenseListItem(int Id, string VoucherNo, DateTime Date, string CategoryName, decimal Amount, string PaidFrom, string? Description);

public record ExpenseCategorySummary(string CategoryName, decimal TodayTotal, decimal MonthToDateTotal, decimal SharePct);
