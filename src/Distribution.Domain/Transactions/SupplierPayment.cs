using Distribution.Domain.Common;
using Distribution.Domain.Masters;

namespace Distribution.Domain.Transactions;

public class SupplierPayment : AuditableEntity
{
    public string VoucherNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public PaidBy PaidBy { get; set; }
    public string? Remarks { get; set; }
}
