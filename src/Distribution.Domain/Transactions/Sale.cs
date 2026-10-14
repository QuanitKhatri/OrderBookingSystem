using Distribution.Domain.Common;
using Distribution.Domain.Masters;

namespace Distribution.Domain.Transactions;

public class Sale : AuditableEntity
{
    public string BillNo { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal ItemDiscount { get; set; }
    public decimal BillDiscount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal CashReceived { get; set; }
    public string? Remarks { get; set; }

    public ICollection<SaleItem> Items { get; set; } = [];
}
