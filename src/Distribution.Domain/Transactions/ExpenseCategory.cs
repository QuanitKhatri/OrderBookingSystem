using Distribution.Domain.Common;

namespace Distribution.Domain.Transactions;

public class ExpenseCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public ExpenseType Type { get; set; }
    public bool IsSystem { get; set; }
}
