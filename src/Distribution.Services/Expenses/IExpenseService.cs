namespace Distribution.Services.Expenses;

public interface IExpenseCategoryService
{
    Task<List<(int Id, string Name)>> GetActiveLookupAsync(CancellationToken ct = default);
    Task<int> CreateAsync(string name, Domain.Common.ExpenseType type, string performedBy, CancellationToken ct = default);
}

public interface IExpenseService
{
    Task RecordAsync(ExpenseInputModel model, string performedBy, CancellationToken ct = default);
    Task<List<ExpenseListItem>> GetTodaysExpensesAsync(CancellationToken ct = default);
    Task<List<ExpenseListItem>> SearchAsync(DateTime? from, DateTime? to, int? categoryId, CancellationToken ct = default);
    Task<List<ExpenseCategorySummary>> GetCategorySummaryAsync(CancellationToken ct = default);
}
