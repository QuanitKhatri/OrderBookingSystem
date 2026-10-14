using Distribution.Domain.Common;

namespace Distribution.Domain.Masters;

public class Customer : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? Phone { get; set; }
    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;
    public string? Address { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal DefaultDiscountPct { get; set; }
}
