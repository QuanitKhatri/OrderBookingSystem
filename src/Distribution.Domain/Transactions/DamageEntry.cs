using Distribution.Domain.Common;
using Distribution.Domain.Masters;

namespace Distribution.Domain.Transactions;

public class DamageEntry : AuditableEntity
{
    public string RefNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public decimal Qty { get; set; }
    public int DamageReasonId { get; set; }
    public DamageReason DamageReason { get; set; } = null!;
    public FoundAt FoundAt { get; set; }
    public decimal CostRate { get; set; }
    public decimal LossAmount { get; set; }
    public string? Remarks { get; set; }
}
