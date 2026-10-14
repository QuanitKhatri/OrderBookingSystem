using Distribution.Domain.Common;

namespace Distribution.Domain.Masters;

public class Area : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<Customer> Customers { get; set; } = [];
}
