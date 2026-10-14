using Distribution.Domain.Common;

namespace Distribution.Domain.Transactions;

public class Expense : AuditableEntity
{
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; } = null!;
    public decimal Amount { get; set; }
    public PaidBy PaidFrom { get; set; }
    public int? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }
    public string? Description { get; set; }
}
