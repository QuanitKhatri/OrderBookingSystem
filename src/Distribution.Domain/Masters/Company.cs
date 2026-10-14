using Distribution.Domain.Common;

namespace Distribution.Domain.Masters;

public class Company : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
