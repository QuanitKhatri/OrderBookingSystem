using Distribution.Domain.Masters;

namespace Distribution.Domain.Transactions;

public class PurchaseItem
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal AvgCostBefore { get; set; }
    public decimal AvgCostAfter { get; set; }
}
