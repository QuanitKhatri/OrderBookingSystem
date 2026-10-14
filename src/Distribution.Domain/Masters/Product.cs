using Distribution.Domain.Common;

namespace Distribution.Domain.Masters;

public class Product : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public UnitType Unit { get; set; }
    public decimal PcsPerUnit { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal SaleRate { get; set; }
    public decimal AvgCost { get; set; }
    public decimal MaxDiscountPct { get; set; }
    public string? Barcode { get; set; }
}
