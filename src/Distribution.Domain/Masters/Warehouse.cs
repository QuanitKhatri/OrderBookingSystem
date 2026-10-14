using Distribution.Domain.Common;

namespace Distribution.Domain.Masters;

public class Warehouse : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
}
