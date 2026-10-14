using Distribution.Domain.Common;
using Distribution.Domain.Masters;

namespace Distribution.Domain.Stock;

public class StockMovement
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public MovementType MovementType { get; set; }
    public string RefType { get; set; } = string.Empty;
    public int RefId { get; set; }
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal UnitCost { get; set; }
}
