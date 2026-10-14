using Distribution.Domain.Common;

namespace Distribution.Domain.Masters;

public class Category : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
