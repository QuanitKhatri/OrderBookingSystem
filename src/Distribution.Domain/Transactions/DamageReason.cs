using Distribution.Domain.Common;

namespace Distribution.Domain.Transactions;

public class DamageReason : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
}
