using Distribution.Data;
using Distribution.Domain.Common;
using Distribution.Domain.Masters;
using Distribution.Services.Sales;
using Distribution.Services.Stock;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Tests;

public class SalesModuleIntegrationTests : IAsyncLifetime
{
    private DistributionDbContext _db = null!;
    private IStockQueryService _stockQuery = null!;
    private ISaleService _saleService = null!;

    private readonly List<int> _productIds = [];
    private int _companyId, _categoryId, _areaId, _customerId;

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("DISTRIBUTION_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=distribution_dev;Username=distribution_app;Password=distribution_dev_pwd";
        var options = new DbContextOptionsBuilder<DistributionDbContext>().UseNpgsql(connectionString).Options;
        _db = new DistributionDbContext(options);

        _stockQuery = new StockQueryService(_db);
        _saleService = new SaleService(_db, _stockQuery, new Distribution.Services.Sequencing.SequenceService(_db), new CustomerBalanceService(_db));

        _companyId = await _db.Companies.Select(c => c.Id).FirstAsync();
        _categoryId = await _db.Categories.Select(c => c.Id).FirstOrDefaultAsync();

        var area = new Area { Name = $"TestArea-{Guid.NewGuid():N}", CreatedAt = DateTime.UtcNow, CreatedBy = "test" };
        _db.Areas.Add(area);
        var customer = new Customer { Code = $"TC-{Guid.NewGuid():N}"[..10], ShopName = "Test Shop", AreaId = 0, OpeningBalance = 1000, CreatedAt = DateTime.UtcNow, CreatedBy = "test" };
        await _db.SaveChangesAsync();
        _areaId = area.Id;
        customer.AreaId = _areaId;
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        _customerId = customer.Id;
    }

    public async Task DisposeAsync()
    {
        var saleItems = await _db.SaleItems.Where(i => _productIds.Contains(i.ProductId)).ToListAsync();
        var saleIds = saleItems.Select(i => i.SaleId).Distinct().ToList();
        _db.SaleItems.RemoveRange(saleItems);

        var movements = await _db.StockMovements.Where(m => _productIds.Contains(m.ProductId)).ToListAsync();
        _db.StockMovements.RemoveRange(movements);

        var receipts = await _db.CustomerReceipts.Where(r => r.CustomerId == _customerId).ToListAsync();
        _db.CustomerReceipts.RemoveRange(receipts);

        var sales = await _db.Sales.Where(s => saleIds.Contains(s.Id)).ToListAsync();
        _db.Sales.RemoveRange(sales);

        var products = await _db.Products.Where(p => _productIds.Contains(p.Id)).ToListAsync();
        _db.Products.RemoveRange(products);

        var customer = await _db.Customers.FindAsync(_customerId);
        if (customer is not null) _db.Customers.Remove(customer);
        var area = await _db.Areas.FindAsync(_areaId);
        if (area is not null) _db.Areas.Remove(area);

        await _db.SaveChangesAsync();
        await _db.DisposeAsync();
    }

    private async Task<int> CreateProductAsync(decimal avgCost, decimal stockQty)
    {
        var product = new Product
        {
            Code = $"TP-{Guid.NewGuid():N}"[..12], Name = "Test Sale Product",
            CompanyId = _companyId, CategoryId = _categoryId, Unit = UnitType.Pcs, PcsPerUnit = 1,
            SaleRate = avgCost * 1.2m, AvgCost = avgCost, MaxDiscountPct = 10,
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        _productIds.Add(product.Id);

        var warehouseId = await _stockQuery.GetDefaultWarehouseIdAsync();
        _db.StockMovements.Add(new Domain.Stock.StockMovement
        {
            Date = DateTime.UtcNow.Date, ProductId = product.Id, WarehouseId = warehouseId,
            MovementType = MovementType.Opening, RefType = "Opening", RefId = 0, QtyIn = stockQty, UnitCost = avgCost
        });
        await _db.SaveChangesAsync();
        return product.Id;
    }

    [Fact]
    public async Task Sale_ComputesDiscountAndNetAmount_AndReducesStock()
    {
        var productId = await CreateProductAsync(avgCost: 50, stockQty: 100);

        var result = await _saleService.CreateAsync(
            new SaleInputModel(_customerId, DateTime.UtcNow.Date,
                [new SaleLineInput(productId, 10, 100m, 10m)], // 10 qty @ 100 = 1000, 10% disc = 100, net = 900
                BillDiscount: 0, CashReceived: 0),
            isAdmin: false, "test");

        Assert.Equal(1000m, result.GrossAmount);
        Assert.Equal(100m, result.ItemDiscount);
        Assert.Equal(900m, result.NetAmount);

        var warehouseId = await _stockQuery.GetDefaultWarehouseIdAsync();
        var remainingStock = await _stockQuery.GetCurrentStockAsync(productId, warehouseId);
        Assert.Equal(90, remainingStock); // 100 - 10
    }

    [Fact]
    public async Task Sale_UpdatesCustomerBalance_AccountingForCashReceived()
    {
        var productId = await CreateProductAsync(avgCost: 50, stockQty: 100);
        var openingBalance = await _saleService.GetCustomerBalanceAsync(_customerId); // 1000 opening

        var result = await _saleService.CreateAsync(
            new SaleInputModel(_customerId, DateTime.UtcNow.Date,
                [new SaleLineInput(productId, 5, 100m, 0m)], // 500 net
                BillDiscount: 0, CashReceived: 200m),
            isAdmin: false, "test");

        Assert.Equal(openingBalance, result.PreviousBalance);
        Assert.Equal(openingBalance + 500m - 200m, result.NewBalance);

        var actualBalance = await _saleService.GetCustomerBalanceAsync(_customerId);
        Assert.Equal(result.NewBalance, actualBalance);
    }

    [Fact]
    public async Task Sale_ExceedingAvailableStock_IsBlocked()
    {
        var productId = await CreateProductAsync(avgCost: 50, stockQty: 5);

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            _saleService.CreateAsync(
                new SaleInputModel(_customerId, DateTime.UtcNow.Date, [new SaleLineInput(productId, 10, 100m, 0m)], 0, 0),
                isAdmin: false, "test"));
    }

    [Fact]
    public async Task Sale_DiscountAboveMax_IsBlockedForNonAdmin_ButAllowedForAdmin()
    {
        var productId = await CreateProductAsync(avgCost: 50, stockQty: 100); // MaxDiscountPct = 10

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _saleService.CreateAsync(
                new SaleInputModel(_customerId, DateTime.UtcNow.Date, [new SaleLineInput(productId, 1, 100m, 50m)], 0, 0),
                isAdmin: false, "test"));

        // Same request succeeds when the caller is an admin (override allowed, per spec 4.2).
        var result = await _saleService.CreateAsync(
            new SaleInputModel(_customerId, DateTime.UtcNow.Date, [new SaleLineInput(productId, 1, 100m, 50m)], 0, 0),
            isAdmin: true, "test");

        Assert.Equal(50m, result.NetAmount); // 100 - 50% discount
    }
}
