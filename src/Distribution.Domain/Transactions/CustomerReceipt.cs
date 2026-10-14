using Distribution.Domain.Common;
using Distribution.Domain.Masters;

namespace Distribution.Domain.Transactions;

public class CustomerReceipt : AuditableEntity
{
    public string ReceiptNo { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public PaidBy PaidBy { get; set; }
    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }
    public string? Remarks { get; set; }
}
