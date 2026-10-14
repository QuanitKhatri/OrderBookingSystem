using Distribution.Domain.Common;
using Distribution.Domain.Masters;

namespace Distribution.Domain.Transactions;

public class Purchase : AuditableEntity
{
    public string InvoiceNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? VehicleNo { get; set; }
    public decimal BiltiCharges { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Remarks { get; set; }

    public ICollection<PurchaseItem> Items { get; set; } = [];
}
